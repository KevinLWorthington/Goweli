using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
using System;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using System.IO;
using System.Collections.ObjectModel;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel : ViewModelBase
    {
        // Core dependencies
        private readonly MainViewModel _mainViewModel;
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
            Console.WriteLine("AddBookViewModel created for WebAssembly");
        }

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

                await Task.Delay(1000); // Simulate processing time

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
            IsPreviewVisible = false;
            Console.WriteLine($"Cover accepted: {_validatedCoverUrl}");
        }

        [RelayCommand]
        private async Task RejectCover()
        {
            _validatedCoverUrl = null;
            IsPreviewVisible = false;
            PreviewCoverUrl = null;
        }
    }
}