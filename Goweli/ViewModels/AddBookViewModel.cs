using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
using Goweli.Data;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Avalonia.Media.Imaging;
using System.IO;
using System.Collections.ObjectModel;
using OpenLibraryNET.Loader;
using System.Collections.Generic;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly GoweliDbContext _dbContext;
        private string? _validatedCoverUrl;
        private readonly HttpClient _client;
        private int _currentCoverIndex = 0;
        private List<string> _coverEditionKeys = new List<string>();
        private TaskCompletionSource<bool>? _userDecisionTcs;
        
        public static ObservableCollection<Book> Books { get; } = new ObservableCollection<Book>();

        // Properties to be called from the view
        [ObservableProperty]
        private string _bookTitle = string.Empty;

        [ObservableProperty]
        private string _authorName = string.Empty;

        [ObservableProperty]
        private string? _iSBN = string.Empty;

        [ObservableProperty]
        private string? _synopsis = string.Empty;

        [ObservableProperty]
        private bool _isChecked;

        [ObservableProperty]
        private Bitmap? _previewCoverImage;

        [ObservableProperty]
        private bool _isPreviewVisible;

        [ObservableProperty]
        private string? _previewCoverUrl;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isProcessingCovers;

        // Constructor for dependency injection
        public AddBookViewModel(MainViewModel mainViewModel, GoweliDbContext dbContext)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _client = new HttpClient();
        }

        // Method to get the book cover URL
        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
                _client.Timeout = TimeSpan.FromSeconds(15);

                // Search for the book title in Open Library API using OpenLibraryNET
                var searchResults = await OLSearchLoader.GetSearchResultsAsync(
                    _client,
                    title,
                    new KeyValuePair<string, string>("limit", "20")
                );

                if (searchResults == null || searchResults.Length == 0)
                {
                    return null;
                }

                _coverEditionKeys.Clear();
                _currentCoverIndex = 0;

                foreach (var result in searchResults)
                {
                    if (result.ExtensionData.TryGetValue("cover_edition_key", out var coverEditionKeyObj) &&
                        coverEditionKeyObj != null && !string.IsNullOrEmpty(coverEditionKeyObj.ToString()))
                    {
                        string coverEditionKey = coverEditionKeyObj.ToString();
                        _coverEditionKeys.Add(coverEditionKey);
                    }
                    else if (result.ExtensionData.TryGetValue("cover_i", out var coverId) &&
                            coverId != null && !string.IsNullOrEmpty(coverId.ToString()))
                    {
                        string coverIdKey = coverId.ToString();
                        _coverEditionKeys.Add("ID:" + coverIdKey);
                    }
                }


                if (_coverEditionKeys.Count == 0)
                {
                    return null;
                }

                await DisplayCoverAtCurrentIndexAsync();

                _userDecisionTcs = new TaskCompletionSource<bool>();
                await _userDecisionTcs.Task;

                return _validatedCoverUrl;            
        }

        // Method to display the book cover
        private async Task DisplayCoverAtCurrentIndexAsync()
        {
            if (_currentCoverIndex >= _coverEditionKeys.Count)
            {
                _validatedCoverUrl = null;
                IsPreviewVisible = false;
                _userDecisionTcs?.TrySetResult(true);
                return;
            }

            try
            {
                IsProcessingCovers = true;
                string coverEditionKey = _coverEditionKeys[_currentCoverIndex];

                // Generate the cover URL based on the cover edition key or cover ID in the search results
                string coverUrl;
                                
                if (coverEditionKey.StartsWith("ID:"))
                {
                    string coverId = coverEditionKey.Substring(3);
                    coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
                }
                else
                {
                    coverUrl = $"https://covers.openlibrary.org/b/olid/{coverEditionKey}-M.jpg";
                }


                var imageResponse = await _client.GetByteArrayAsync(coverUrl);

                if (imageResponse.Length < 1000)
                {
                    _currentCoverIndex++;
                    await DisplayCoverAtCurrentIndexAsync();
                    return;
                }
                // Load the cover image in memory
                using var memoryStream = new MemoryStream(imageResponse);
                PreviewCoverImage = new Bitmap(memoryStream);
                PreviewCoverUrl = coverUrl;

                IsPreviewVisible = true;
            }
            catch (Exception)
            {

                await Task.Delay(500);

                _currentCoverIndex++;
                await DisplayCoverAtCurrentIndexAsync();
            }
            finally
            {
                IsProcessingCovers = false;
            }
        }

        // Method to submit the book
        [RelayCommand]
        private async Task Submit()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(BookTitle) || string.IsNullOrWhiteSpace(AuthorName))
                {
                    StatusMessage = "Error: Author and Title are required.";
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                    return;
                }

                StatusMessage = "Searching for book cover...";

                await GetBookCoverUrlAsync(BookTitle);

                var newBook = new Book
                {
                    BookTitle = this.BookTitle,
                    AuthorName = this.AuthorName,
                    ISBN = this.ISBN,
                    Synopsis = this.Synopsis,
                    IsChecked = this.IsChecked,
                    CoverUrl = _validatedCoverUrl
                };

                _dbContext.Books.Add(newBook);
                await _dbContext.SaveChangesAsync();

                StatusMessage = "Book added successfully!";
                await Task.Delay(2000);

                BookTitle = string.Empty;
                AuthorName = string.Empty;
                ISBN = string.Empty;
                Synopsis = string.Empty;
                IsChecked = false;

                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {

                StatusMessage = $"Error: {ex.Message}";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
        }

        // Method to accept the book cover
        [RelayCommand]
        private void AcceptCover()
        {
            _validatedCoverUrl = PreviewCoverUrl;
            IsPreviewVisible = false;
            _userDecisionTcs?.TrySetResult(true);
        }

        // Method to reject the book cover and try the next one
        [RelayCommand]
        private async Task RejectCover()
        {
            _currentCoverIndex++;
            if (_currentCoverIndex < _coverEditionKeys.Count)
            {
                StatusMessage = "Loading next cover...";
                await DisplayCoverAtCurrentIndexAsync();
                StatusMessage = string.Empty;
            }
            else
            {
                _validatedCoverUrl = null;
                IsPreviewVisible = false;
                _userDecisionTcs?.TrySetResult(true);
            }
        }
    }
}


