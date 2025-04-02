using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using Avalonia.Media.Imaging;
using System.IO;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Net.Http.Json;
using Goweli.Services;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel(MainViewModel mainViewModel, IDatabaseService databaseService, HttpClient? apiClient = null) : ViewModelBase
    {
        // Dependency injections fields
        private readonly MainViewModel _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        private readonly IDatabaseService _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
        private readonly HttpClient _apiClient = apiClient ?? new HttpClient();
        private string? _validatedCoverUrl;
        private int _currentCoverIndex = 0; // Index of book covers to be cycled through (should start at 0)
        private List<CoverSource> _coverSources = [];
        private TaskCompletionSource<bool>? _userDecisionTcs;

        // Base URL for API. Replace with actual URL in production
        private readonly string _apiBaseUrl = "http://localhost:5128/api/Proxy";

        public static ObservableCollection<Book> Books { get; } = [];

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

        // Method to get book covers from the API
        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                StatusMessage = "Searching for book covers...";

                // Get cover sources from the API
                var requestUrl = $"{_apiBaseUrl}/covers?title={Uri.EscapeDataString(title)}";
                var response = await _apiClient.GetAsync(requestUrl);

                // Update user if there's an error with the API
                if (!response.IsSuccessStatusCode)
                {
                    StatusMessage = $"Failed to find covers for this book. Status: {response.StatusCode}";
                    return null;
                }

                // Get a list of covers to be loaded for the user to select from
                _coverSources = await response.Content.ReadFromJsonAsync<List<CoverSource>>() ?? [];

                if (_coverSources.Count == 0) // If there's no covers available, notify the user and set the cover to null
                {
                    StatusMessage = "No covers found for this book.";
                    return null;
                }

                await DisplayCoverAtCurrentIndexAsync();

                // Create a new TaskCompletionSource to wait for user's decision
                _userDecisionTcs = new TaskCompletionSource<bool>();

                // Wait for the user to make a decision
                await _userDecisionTcs.Task;

                // Once the user has made a decision, return the validated cover URL to be stored (or null if no cover chosen)
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
                IsProcessingCovers = true; // Bound in the view to update the UI
                var coverSource = _coverSources[_currentCoverIndex];
                string? coverUrl = coverSource.Url;

                // Validate the cover URL using the API
                var validateUrl = $"{_apiBaseUrl}/validateCover?coverUrl={Uri.EscapeDataString(coverUrl)}";
                var validateResponse = await _apiClient.GetAsync(validateUrl);

                if (!validateResponse.IsSuccessStatusCode)
                {
                    // Move to the next cover if this one is invalid
                    StatusMessage = $"Cover {_currentCoverIndex + 1} is invalid, trying next...";
                    _currentCoverIndex++; // Move to the next cover
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

                IsPreviewVisible = true; // Bound in the view to update the UI
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
                // If the user has not entered a title and/or author, remind them to do so
                if (string.IsNullOrWhiteSpace(BookTitle) || string.IsNullOrWhiteSpace(AuthorName))
                {
                    StatusMessage = "Error: Author and Title are required.";
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                    return;
                }

                StatusMessage = "Searching for book cover..."; // Show that we're searching for a cover image

                // Get cover URL and wait for user selection
                string? coverUrl = await GetBookCoverUrlAsync(BookTitle);

                // Use the selected cover URL and set all book info to the database (cover may be null if user rejected all covers)
                var newBook = new Book
                {
                    BookTitle = this.BookTitle,
                    AuthorName = this.AuthorName,
                    ISBN = this.ISBN,
                    Synopsis = this.Synopsis,
                    IsChecked = this.IsChecked,
                    CoverUrl = coverUrl  // This will be the URL the user selected or null
                };

                await _databaseService.AddBookAsync(newBook);

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
            _validatedCoverUrl = PreviewCoverUrl; // Set the validated cover URL to the preview that the user accepted
            IsPreviewVisible = false; // Reset UI
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
        public string? Type { get; set; } // "olid" or "id"
        public string? Key { get; set; }
        public string? Url { get; set; }
    }

    public class ValidateCoverResponse
    {
        public bool IsValid { get; set; }
        public byte[]? ImageBytes { get; set; }
    }
}