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
using System.Collections.Generic;
using System.Net.Http.Json;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly GoweliDbContext _dbContext;
        private readonly HttpClient _apiClient;
        private string? _validatedCoverUrl;
        private int _currentCoverIndex = 0;
        private List<CoverSource> _coverSources = new List<CoverSource>();
        private TaskCompletionSource<bool>? _userDecisionTcs;

        // Base URL for API. Replace with actual URL in production
        private readonly string _apiBaseUrl = "http://localhost:5128/api/Proxy";

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
        public AddBookViewModel(MainViewModel mainViewModel, GoweliDbContext dbContext, HttpClient apiClient = null)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

            // Create a new HttpClient if not injected
            _apiClient = apiClient ?? new HttpClient();
        }

        // Method to get book covers from the API
        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                StatusMessage = "Searching for book covers...";

                // Get cover sources from the API
                var requestUrl = $"{_apiBaseUrl}/covers?title={Uri.EscapeDataString(title)}";
                var response = await _apiClient.GetAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    StatusMessage = $"Failed to find covers for this book. Status: {response.StatusCode}";
                    return null;
                }

                _coverSources = await response.Content.ReadFromJsonAsync<List<CoverSource>>() ?? new List<CoverSource>();

                if (_coverSources.Count == 0)
                {
                    StatusMessage = "No covers found for this book.";
                    return null;
                }

                _currentCoverIndex = 0;
                await DisplayCoverAtCurrentIndexAsync();

                // Create a new TaskCompletionSource to wait for user's decision
                _userDecisionTcs = new TaskCompletionSource<bool>();

                // Wait for the user to make a decision
                await _userDecisionTcs.Task;

                return _validatedCoverUrl;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error searching for covers: {ex.Message}";
                return null;
            }
        }

        // Method to display the book cover
        private async Task DisplayCoverAtCurrentIndexAsync()
        {
            if (_currentCoverIndex >= _coverSources.Count)
            {
                _validatedCoverUrl = null;
                IsPreviewVisible = false;
                _userDecisionTcs?.TrySetResult(true);
                return;
            }

            try
            {
                IsProcessingCovers = true;
                var coverSource = _coverSources[_currentCoverIndex];
                string coverUrl = coverSource.Url;

                // Validate the cover URL using the API
                var validateUrl = $"{_apiBaseUrl}/validateCover?coverUrl={Uri.EscapeDataString(coverUrl)}";
                var validateResponse = await _apiClient.GetAsync(validateUrl);

                if (!validateResponse.IsSuccessStatusCode)
                {
                    // Move to the next cover if this one is invalid
                    StatusMessage = $"Cover {_currentCoverIndex + 1} is invalid, trying next...";
                    _currentCoverIndex++;
                    await DisplayCoverAtCurrentIndexAsync();
                    return;
                }

                // Get the image bytes from the response
                var responseContent = await validateResponse.Content.ReadFromJsonAsync<ValidateCoverResponse>();

                if (responseContent == null || !responseContent.IsValid || responseContent.ImageBytes.Length < 1000)
                {
                    // Move to the next cover if this one is invalid
                    _currentCoverIndex++;
                    await DisplayCoverAtCurrentIndexAsync();
                    return;
                }

                // Load the cover image in memory
                using var memoryStream = new MemoryStream(responseContent.ImageBytes);
                PreviewCoverImage = new Bitmap(memoryStream);
                PreviewCoverUrl = coverUrl;

                IsPreviewVisible = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading cover: {ex.Message}";
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

                // Get cover URL and wait for user selection
                string? coverUrl = await GetBookCoverUrlAsync(BookTitle);

                // Use the selected cover URL (may be null if user rejected all covers)
                var newBook = new Book
                {
                    BookTitle = this.BookTitle,
                    AuthorName = this.AuthorName,
                    ISBN = this.ISBN,
                    Synopsis = this.Synopsis,
                    IsChecked = this.IsChecked,
                    CoverUrl = coverUrl  // This will be the URL the user selected or null
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
                _validatedCoverUrl = null;

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
            if (_currentCoverIndex < _coverSources.Count)
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

    // Helper classes for JSON deserialization
    public class CoverSource
    {
        public string Type { get; set; } // "olid" or "id"
        public string Key { get; set; }
        public string Url { get; set; }
    }

    public class ValidateCoverResponse
    {
        public bool IsValid { get; set; }
        public byte[] ImageBytes { get; set; }
    }
}