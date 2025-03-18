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
                _client.Timeout = TimeSpan.FromSeconds(15); // Increased timeout
                Console.WriteLine($"Searching for book with title: {title}");

                var searchResults = await OLSearchLoader.GetSearchResultsAsync(
                    _client,
                    title,
                    new KeyValuePair<string, string>("limit", "20") // Increased limit
                );

                if (searchResults == null || searchResults.Length == 0)
                {
                    Console.WriteLine("No works found for the given title");
                    return null;
                }

                _coverEditionKeys.Clear();
                _currentCoverIndex = 0;

                Console.WriteLine($"Found {searchResults.Length} search results for '{title}'");

                // Extract cover edition keys from all search results
                foreach (var result in searchResults)
                {
                    // First try cover_edition_key
                    if (result.ExtensionData.TryGetValue("cover_edition_key", out var coverEditionKeyObj) &&
                        coverEditionKeyObj != null && !string.IsNullOrEmpty(coverEditionKeyObj.ToString()))
                    {
                        string coverEditionKey = coverEditionKeyObj.ToString();
                        Console.WriteLine($"Found cover_edition_key: {coverEditionKey}");
                        _coverEditionKeys.Add(coverEditionKey);
                    }
                    // Then try cover_i as fallback
                    else if (result.ExtensionData.TryGetValue("cover_i", out var coverId) &&
                            coverId != null && !string.IsNullOrEmpty(coverId.ToString()))
                    {
                        string coverIdKey = coverId.ToString();
                        Console.WriteLine($"Found cover_i: {coverIdKey}");
                        // We'll handle this differently when displaying
                        _coverEditionKeys.Add("ID:" + coverIdKey); // Prefixing with ID: to distinguish from edition keys
                    }
                }

                Console.WriteLine($"Extracted {_coverEditionKeys.Count} potential covers");

                if (_coverEditionKeys.Count == 0)
                {
                    Console.WriteLine("No cover keys found in search results");
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
                Console.WriteLine($"No more covers available. Reached index {_currentCoverIndex} of {_coverEditionKeys.Count}");
                _validatedCoverUrl = null;
                IsPreviewVisible = false;
                _userDecisionTcs?.TrySetResult(true);
                return;
            }

            try
            {
                IsProcessingCovers = true;
                string coverEditionKey = _coverEditionKeys[_currentCoverIndex];
                Console.WriteLine($"Attempting to load cover {_currentCoverIndex + 1}/{_coverEditionKeys.Count}: Edition key = {coverEditionKey}");

                string coverUrl;
                if (coverEditionKey.StartsWith("ID:"))
                {
                    // Handle cover_i format
                    string coverId = coverEditionKey.Substring(3);
                    coverUrl = $"https://covers.openlibrary.org/b/id/{coverId}-M.jpg";
                }
                else
                {
                    // Handle cover_edition_key format
                    coverUrl = $"https://covers.openlibrary.org/b/olid/{coverEditionKey}-M.jpg";
                }

                Console.WriteLine($"Generated cover URL: {coverUrl}");

                // Add a validation check to prevent loading "no cover" placeholder
                var imageResponse = await _client.GetByteArrayAsync(coverUrl);

                // Check if the image is the "no cover" placeholder (usually very small)
                if (imageResponse.Length < 1000)
                {
                    Console.WriteLine($"Cover appears to be a placeholder (size: {imageResponse.Length} bytes). Skipping to next.");
                    _currentCoverIndex++;
                    await DisplayCoverAtCurrentIndexAsync();
                    return;
                }

                using var memoryStream = new MemoryStream(imageResponse);
                PreviewCoverImage = new Bitmap(memoryStream);
                PreviewCoverUrl = coverUrl;

                Console.WriteLine($"Successfully loaded cover image from: {coverUrl}");
                IsPreviewVisible = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading cover at index {_currentCoverIndex}: {ex.Message}");

                // Add a small delay before trying the next cover to avoid rapid failures
                await Task.Delay(500);

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


