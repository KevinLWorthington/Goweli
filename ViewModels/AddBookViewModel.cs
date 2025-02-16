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
        private string? _coverUrl = string.Empty;

        // Cover preview properties - these manage the cover image preview functionality
        [ObservableProperty]
        private string? _previewCoverUrl;

        [ObservableProperty]
        private bool _isPreviewVisible;

        // UI state properties
        [ObservableProperty]
        private string _buttonText = string.Empty;

        // Commands that handle user interactions
        public IAsyncRelayCommand SubmitCommand { get; }
        public IAsyncRelayCommand AcceptCoverCommand { get; }
        public IAsyncRelayCommand RejectCoverCommand { get; }

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
            if (string.IsNullOrWhiteSpace(BookTitle) || string.IsNullOrWhiteSpace(AuthorName))
            {
                await _dialogService.ShowDialog(
                    "Author and Title Required.",
                    "ERROR",
                    new List<DialogButton> { new("OK", false) });
                return;
            }

            var coverUrl = await GetBookCoverUrlAsync(BookTitle);

            if (coverUrl == null && !_continueWithNullCover)
            {
                return;
            }

            try
            {
                var newBook = new Book
                {
                    BookTitle = this.BookTitle,
                    AuthorName = this.AuthorName,
                    ISBN = this.ISBN,
                    Synopsis = this.Synopsis,
                    IsChecked = this.IsChecked,
                    CoverUrl = _validatedCoverUrl // Use the validated URL
                };

                using var db = new AppDbContext();
                db.Books.Add(newBook);
                db.SaveChanges();

                ButtonText = "Book Added!";
                await Task.Delay(2000);
                _mainViewModel.ShowDefaultView();
            }
            catch (Exception ex)
            {
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

        // Models for deserializing the Open Library API response
        public class OpenLibrarySearchResult
        {
            [JsonPropertyName("docs")]
            public List<OpenLibraryDoc>? Docs { get; set; }

            [JsonPropertyName("numFound")]
            public int NumFound { get; set; }
        }

        public class OpenLibraryDoc
        {
            [JsonPropertyName("cover_i")]
            public int? CoverI { get; set; }

            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("author_name")]
            public List<string>? AuthorNames { get; set; }

            [JsonPropertyName("isbn")]
            public List<string>? ISBNs { get; set; }
        }

        // Fetches and validates the book cover from the Open Library API
        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                // Search for works matching the title
                // We'll use the search functionality to find the work first
                var workResults = await OLSearchLoader.GetSearchResultsAsync(
                    _client,
                    "",  // Empty query string because we're using parameters
                    new KeyValuePair<string, string>("title", title),
                    new KeyValuePair<string, string>("limit", "1")
                );

                if (workResults?.Length > 0)
                {
                    // Get the first work's ID
                    string workOLID = workResults[0].Key;

                    // Get the first edition of this work
                    var editions = await OLWorkLoader.GetEditionsAsync(
                        _client,
                        workOLID,
                        new KeyValuePair<string, string>("limit", "1")
                    );

                    if (editions?.Length > 0)
                    {
                        var firstEdition = editions[0];

                        // Try to get the cover image using the edition's OLID
                        try
                        {
                            // Download the actual cover image bytes
                            byte[] coverBytes = await OLImageLoader.GetCoverAsync(
                                _client,
                                CoverIdType.OLID,
                                firstEdition.ID,
                                ImageSize.Medium
                            );

                            // If we got here, the cover exists. Construct the URL for display
                            var coverUrl = $"https://covers.openlibrary.org/b/olid/{firstEdition.ID}-M.jpg";

                            PreviewCoverUrl = coverUrl;
                            IsPreviewVisible = true;
                            return coverUrl;
                        }
                        catch
                        {
                            // If getting the cover failed, we'll try using the work's cover as fallback
                            try
                            {
                                byte[] workCoverBytes = await OLImageLoader.GetCoverAsync(
                                    _client,
                                    CoverIdType.OLID,
                                    workOLID,
                                    ImageSize.Medium
                                );

                                var coverUrl = $"https://covers.openlibrary.org/b/olid/{workOLID}-M.jpg";

                                PreviewCoverUrl = coverUrl;
                                IsPreviewVisible = true;
                                return coverUrl;
                            }
                            catch
                            {
                                // Both attempts failed, proceed without cover
                                _continueWithNullCover = true;
                                return null;
                            }
                        }
                    }
                }

                // If we couldn't find any editions or works, proceed without cover
                _continueWithNullCover = true;
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching book cover: {ex.Message}");
                _continueWithNullCover = true;
                return null;
            }
            finally
            {
                if (!(_validatedCoverUrl != null && _validatedCoverUrl == PreviewCoverUrl))
                {
                    IsPreviewVisible = false;
                    PreviewCoverUrl = null;
                }
            }
        }

        // Helper method to show the "continue without cover" dialog
        /* private async Task<bool> ShowContinueWithoutCoverDialog(string message)
         {
             var result = await _dialogService.ShowDialog(
                 $"{message}\nWould you like to continue without a cover image?",
                 "Cover Image Issue",
                 new List<DialogButton> {
                     new("Continue without cover", true),
                     new("Cancel submission", false)
                 });

             return result.Result ?? false;
         } */

        // Handlers for cover validation commands
        private async Task OnAcceptCover()
        {
            _validatedCoverUrl = PreviewCoverUrl;
            IsPreviewVisible = false;
        }

        private async Task OnRejectCover()
        {
            _validatedCoverUrl = null;
            IsPreviewVisible = false;
            _continueWithNullCover = true; //await ShowContinueWithoutCoverDialog("Cover image rejected.");
        }
    }
}
