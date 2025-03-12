using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
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
        private bool _continueWithNullCover = false;
        private string? _validatedCoverUrl;
        private readonly HttpClient _client;

        // Static collection to store books in memory if database is not available
        public static ObservableCollection<Book> Books { get; } = new ObservableCollection<Book>();

        // Book information properties
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

        // Constructor initializes the view model and sets up commands
        public AddBookViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
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
                Console.WriteLine($"Error in GetBookCoverUrlAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            _continueWithNullCover = true;
            _validatedCoverUrl = null;
            return null;
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

                ButtonText = "Searching...";
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

                // Add to in-memory collection
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

                ButtonText = "Submit";
                StatusMessage = $"Error: {ex.Message}";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
        }

        // Handlers for cover validation commands
        [RelayCommand]
        private void AcceptCover()
        {
            _validatedCoverUrl = PreviewCoverUrl;
            IsPreviewVisible = false;
        }

        [RelayCommand]
        private void RejectCover()
        {
            _validatedCoverUrl = null;
            _continueWithNullCover = true;
            IsPreviewVisible = false;
        }
    }
}


