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
using Microsoft.EntityFrameworkCore;

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

        public AddBookViewModel(MainViewModel mainViewModel, GoweliDbContext dbContext)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _client = new HttpClient();
            Console.WriteLine("AddBookViewModel created");
        }

        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                _client.Timeout = TimeSpan.FromSeconds(10);
                Console.WriteLine($"Searching for book with title: {title}");

                var searchResults = await OLSearchLoader.GetSearchResultsAsync(
                    _client,
                    title,
                    new KeyValuePair<string, string>("limit", "10")
                );

                if (searchResults == null || searchResults.Length == 0)
                {
                    Console.WriteLine("No works found for the given title");
                    return null;
                }

                _coverEditionKeys.Clear();
                _currentCoverIndex = 0;

                foreach (var result in searchResults)
                {
                    if (result.ExtensionData.TryGetValue("cover_edition_key", out var coverEditionKeyObj) &&
                        coverEditionKeyObj != null)
                    {
                        string coverEditionKey = coverEditionKeyObj.ToString();
                        if (!string.IsNullOrEmpty(coverEditionKey))
                        {
                            _coverEditionKeys.Add(coverEditionKey);
                        }
                    }
                }

                Console.WriteLine($"Found {_coverEditionKeys.Count} potential covers");

                if (_coverEditionKeys.Count == 0)
                {
                    Console.WriteLine("No cover_edition_keys found in search results");
                    return null;
                }

                await DisplayCoverAtCurrentIndexAsync();

                _userDecisionTcs = new TaskCompletionSource<bool>();

                await _userDecisionTcs.Task;

                return _validatedCoverUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBookCoverUrlAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        private async Task DisplayCoverAtCurrentIndexAsync()
        {
            if (_currentCoverIndex >= _coverEditionKeys.Count)
            {
                Console.WriteLine("No more covers available");
                _validatedCoverUrl = null;
                IsPreviewVisible = false;

                _userDecisionTcs?.TrySetResult(true);
                return;
            }

            try
            {
                IsProcessingCovers = true;
                string coverEditionKey = _coverEditionKeys[_currentCoverIndex];
                string coverUrl = $"https://covers.openlibrary.org/b/olid/{coverEditionKey}-M.jpg";

                Console.WriteLine($"Showing cover {_currentCoverIndex + 1} of {_coverEditionKeys.Count}: {coverUrl}");

                var imageResponse = await _client.GetByteArrayAsync(coverUrl);
                using var memoryStream = new MemoryStream(imageResponse);

                PreviewCoverImage = new Bitmap(memoryStream);
                PreviewCoverUrl = coverUrl;

                Console.WriteLine($"Successfully loaded cover image from: {coverUrl}");
                IsPreviewVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cover: {ex.Message}");
                _currentCoverIndex++;
                await DisplayCoverAtCurrentIndexAsync();
            }
            finally
            {
                IsProcessingCovers = false;
            }
        }

        // Simplified book submission
        [RelayCommand]
        private async Task Submit()
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(BookTitle) || string.IsNullOrWhiteSpace(AuthorName))
                {
                    StatusMessage = "Error: Author and Title are required.";
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                    return;
                }

                StatusMessage = "Searching for book cover...";

                await GetBookCoverUrlAsync(BookTitle);

                // Create a new book
                var newBook = new Book
                {
                    Id = Books.Count + 1,
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

                // Clear fields after adding
                BookTitle = string.Empty;
                AuthorName = string.Empty;
                ISBN = string.Empty;
                Synopsis = string.Empty;
                IsChecked = false;

                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during submission
                Console.WriteLine($"Error in Submit: {ex.Message}");

                StatusMessage = $"Error: {ex.Message}";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        private void AcceptCover()
        {
            _validatedCoverUrl = PreviewCoverUrl;
            IsPreviewVisible = false;
            _userDecisionTcs?.TrySetResult(true);
        }

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


