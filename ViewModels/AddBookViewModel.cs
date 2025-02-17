using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Goweli.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Json;
using System.Linq;
using System.Text.Json.Serialization;
using OpenLibraryNET;
using OpenLibraryNET.Data;
using OpenLibraryNET.Loader;
using OpenLibraryNET.Utility;
using System.Linq.Expressions;
using System.Diagnostics;
using Avalonia.Controls.Chrome;
using Avalonia.Media.Imaging;
using System.IO;

namespace Goweli.ViewModels
{
    // This view model handles the addition of new books to the database,
    // including fetching and validating cover images from the Open Library API
    public partial class AddBookViewModel : ViewModelBase
    {

        private readonly HttpClient _client;
        private readonly OpenLibraryClient _olClient;

        // Core dependencies
        private readonly MainViewModel _mainViewModel;
        private readonly DialogService _dialogService;
                
        private bool _continueWithNullCover = false;
        private string? _validatedCoverUrl;

        // Book information properties - these connect the UI input fields to our data model
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

        // UI state properties
        [ObservableProperty]
        private string _buttonText = string.Empty;

        // Commands that handle user interactions
        public IAsyncRelayCommand SubmitCommand { get; }
        public IAsyncRelayCommand AcceptCoverCommand { get; }
        public IAsyncRelayCommand RejectCoverCommand { get; }

        private async Task<Bitmap?> LoadImageFromUrl(string url)
        {
            try
            {
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(url);

                using var memoryStream = new MemoryStream(imageBytes);
                return new Bitmap(memoryStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                return null;
            }
        }

        // Constructor initializes the view model and sets up commands
        public AddBookViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _dialogService = new DialogService();

            _client = new HttpClient();
            _olClient = new OpenLibraryClient();

            SubmitCommand = new AsyncRelayCommand(OnSubmit);
            AcceptCoverCommand = new AsyncRelayCommand(OnAcceptCover);
            RejectCoverCommand = new AsyncRelayCommand(OnRejectCover);
            ButtonText = "Submit";
        }

        // Handles the book submission process
        private async Task OnSubmit()
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(BookTitle) || string.IsNullOrWhiteSpace(AuthorName))
                {
                    await _dialogService.ShowDialog(
                        "Author and Title Required.",
                        "ERROR",
                        new List<DialogButton> { new("OK", false) });
                    return;
                }

                // Update UI to show we're searching
                ButtonText = "Searching...";

                // Try to get a cover image
                var coverUrl = await GetBookCoverUrlAsync(BookTitle);

                // Check if we should continue without a cover
                if (coverUrl == null && !_continueWithNullCover)
                {
                    ButtonText = "Submit";
                    return;
                }

                while (IsPreviewVisible)
                {
                    await Task.Delay(100);
                }

                // Update UI to show we're saving
                ButtonText = "Adding Book...";

                // Create and save the new book
                var newBook = new Book
                {
                    BookTitle = this.BookTitle,
                    AuthorName = this.AuthorName,
                    ISBN = this.ISBN,
                    Synopsis = this.Synopsis,
                    IsChecked = this.IsChecked,
                    CoverUrl = _validatedCoverUrl
                };

                using var db = new AppDbContext();
                db.Books.Add(newBook);
                await db.SaveChangesAsync();

                // Show success and return to main view
                ButtonText = "Book Added!";
                await Task.Delay(2000);
                _mainViewModel.ShowDefaultView();
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during submission
                Debug.WriteLine($"Error in OnSubmit: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                ButtonText = "Submit";

                string errorMessage = $"An error occurred while submitting the book: {ex.Message}";
                if (ex.InnerException != null)
                {
                    errorMessage += $"\nInner Exception: {ex.InnerException.Message}";
                }

                await _dialogService.ShowDialog(
                    errorMessage,
                    "Error",
                    new List<DialogButton> { new("OK", false) });
            }
        }





        // Fetches and validates the book cover from the Open Library API
        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);
                httpClient.DefaultRequestHeaders.Add("User-Agent", "Goweli Book Application/1.0");

                Console.WriteLine($"Searching for book with title: {title}");

                var searchResults = await OLSearchLoader.GetSearchResultsAsync(
                    httpClient,
                    title,
                    new KeyValuePair<string, string>("limit", "1")
                );

                if (searchResults == null || searchResults.Length == 0)
                {
                    Console.WriteLine("No works found for the given title");
                    _continueWithNullCover = true;
                    return null;
                }

                var firstResult = searchResults[0];

                if (firstResult.ExtensionData.TryGetValue("cover_edition_key", out var coverEditionKeyObj) &&
                    coverEditionKeyObj != null)
                {
                    string coverEditionKey = coverEditionKeyObj.ToString();
                    Console.WriteLine($"Found cover edition key: {coverEditionKey}");

                    // Construct the URL for the cover
                    var coverUrl = $"https://covers.openlibrary.org/b/olid/{coverEditionKey}-M.jpg";

                    try
                    {
                        // Download and create the Bitmap for display
                        var imageResponse = await httpClient.GetByteArrayAsync(coverUrl);
                        using var memoryStream = new MemoryStream(imageResponse);

                        // Set both the Bitmap for display and the URL for storage
                        PreviewCoverImage = new Bitmap(memoryStream);
                        PreviewCoverUrl = coverUrl;

                        Console.WriteLine($"Successfully loaded cover image from: {coverUrl}");
                        IsPreviewVisible = true;
                        _validatedCoverUrl = coverUrl;
                        return coverUrl;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to load image: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("No cover_edition_key found in search results");
                }

                Console.WriteLine("No valid cover found, continuing without cover");
                _continueWithNullCover = true;
                _validatedCoverUrl = null;
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBookCoverUrlAsync: {ex.GetType().Name}");
                Console.WriteLine($"Message: {ex.Message}");

                _continueWithNullCover = true;
                _validatedCoverUrl = null;
                return null;
            }
        }

        // Handlers for cover validation commands
        private async Task OnAcceptCover()
        {
            IsPreviewVisible = false;
            Debug.WriteLine($"Cover accepted: {_validatedCoverUrl}");
        }

        private async Task OnRejectCover()
        {
            _validatedCoverUrl = null;
            IsPreviewVisible = false;
            PreviewCoverUrl = null;
            _continueWithNullCover = true;
        }
    }
}
