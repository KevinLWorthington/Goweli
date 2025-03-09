using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using OpenLibraryNET;
using OpenLibraryNET.Data;
using OpenLibraryNET.Loader;
using OpenLibraryNET.Utility;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using System.IO;
using System.Collections.ObjectModel;
using static OpenLibraryNET.Data.OLPartnerData.Item;
using System.Collections.Generic;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private bool _continueWithNullCover = false;
        private string? _validatedCoverUrl;

        private readonly HttpClient _client;
        private readonly OpenLibraryClient _olClient;

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

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // UI state properties
        [ObservableProperty]
        private string _buttonText = "Submit";

        // A static collection to hold books (in-memory storage for WebAssembly)
        public static ObservableCollection<Book> Books { get; } = new ObservableCollection<Book>();

        // Constructor initializes the view model and sets up commands
        public AddBookViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _client = new HttpClient();
            // _olClient = new OpenLibraryClient();
            Console.WriteLine("AddBookViewModel created for WebAssembly");
        }

       /* private async Task<Bitmap?> LoadImageFromUrl(string url)
        {
            try
            {
                var imageBytes = await _client.GetByteArrayAsync(url);

                using var memoryStream = new MemoryStream(imageBytes);
                return new Bitmap(memoryStream);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading image: {ex.Message}");
                return null;
            }
        } */

        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                _client.Timeout = TimeSpan.FromSeconds(10);
                //_client.DefaultRequestHeaders.Add("User-Agent", "Goweli Book Application/1.0");

                Console.WriteLine($"Searching for book with title: {title}");

                var searchResults = await OLSearchLoader.GetSearchResultsAsync(
                    _client,
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
                        var imageResponse = await _client.GetByteArrayAsync(coverUrl);
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

        // Handles the book submission process
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

                // Update UI to show we're searching
                ButtonText = "Searching...";
                StatusMessage = "Searching for book cover...";

                await GetBookCoverUrlAsync(BookTitle);

                // Update UI to show we're saving
                ButtonText = "Adding Book...";
                StatusMessage = "Adding book to library...";

                // Simulate adding a book
                await Task.Delay(1000);

                // Create a new book object
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

                // Add to our in-memory collection
                Books.Add(newBook);

                ButtonText = "Book Added!";
                StatusMessage = "Book added successfully!";
                await Task.Delay(2000);

                // Clear fields after adding
                BookTitle = string.Empty;
                AuthorName = string.Empty;
                ISBN = string.Empty;
                Synopsis = string.Empty;
                IsChecked = false;

                ButtonText = "Submit";
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during submission
                Console.WriteLine($"Error in Submit: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                ButtonText = "Submit";
                StatusMessage = $"Error: {ex.Message}";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
        }

        // Handlers for cover validation commands
        [RelayCommand]
        private async Task AcceptCover()
        {
            _validatedCoverUrl = PreviewCoverUrl;
            IsPreviewVisible = false;
            Console.WriteLine($"Cover accepted: {_validatedCoverUrl}");
        }

        [RelayCommand]
        private async Task RejectCover()
        {
            _validatedCoverUrl = null;
            _continueWithNullCover = true;
        }
    }
}



