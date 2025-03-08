using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Goweli.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Diagnostics;
using Avalonia.Media.Imaging;
using System.IO;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel : ViewModelBase
    {
        // Core dependencies
        private readonly MainViewModel _mainViewModel;
        private readonly DialogService _dialogService;
        private readonly bool _isWebAssembly;

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

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        // UI state properties
        [ObservableProperty]
        private string _buttonText = "Submit";

        // Constructor initializes the view model and sets up commands
        public AddBookViewModel(MainViewModel mainViewModel, bool isWebAssembly = false)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _dialogService = new DialogService();
            _isWebAssembly = isWebAssembly;

            Console.WriteLine($"AddBookViewModel created with isWebAssembly={isWebAssembly}");
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
                    if (_isWebAssembly)
                    {
                        StatusMessage = "Error: Author and Title are required.";
                        await Task.Delay(2000);
                        StatusMessage = string.Empty;
                    }
                    else
                    {
                        await _dialogService.ShowDialog(
                            "Author and Title Required.",
                            "ERROR",
                            new List<DialogButton> { new("OK", false) });
                    }
                    return;
                }

                // Update UI to show we're searching
                ButtonText = "Searching...";
                StatusMessage = "Searching for book cover...";

                // Try to get a cover image if we're not in WebAssembly
                if (!_isWebAssembly)
                {
                    var coverUrl = await SearchForCoverUrlAsync(BookTitle);

                    // Check if we should continue without a cover
                    if (coverUrl == null && !_continueWithNullCover)
                    {
                        ButtonText = "Submit";
                        StatusMessage = string.Empty;
                        return;
                    }

                    while (IsPreviewVisible)
                    {
                        await Task.Delay(100);
                    }
                }

                // Update UI to show we're saving
                ButtonText = "Adding Book...";
                StatusMessage = "Adding book to library...";

                try
                {
                    if (_isWebAssembly)
                    {
                        // In WebAssembly, just simulate adding a book
                        Console.WriteLine($"WebAssembly - Simulating book addition: {BookTitle} by {AuthorName}");
                        await Task.Delay(1000); // Simulate processing time

                        ButtonText = "Book Added!";
                        StatusMessage = "Book added successfully! (WebAssembly Demo Mode)";
                        await Task.Delay(2000);

                        // Clear fields after "adding"
                        BookTitle = string.Empty;
                        AuthorName = string.Empty;
                        ISBN = string.Empty;
                        Synopsis = string.Empty;
                        IsChecked = false;

                        ButtonText = "Submit";
                        StatusMessage = string.Empty;
                    }
                    else
                    {
                        // Create and save the new book using database (desktop only)
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
                        StatusMessage = "Book added successfully!";
                        await Task.Delay(2000);
                        _mainViewModel.ShowDefaultCommand.Execute(null);
                    }
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Database error: {dbEx.Message}");
                    ButtonText = "Submit";

                    if (_isWebAssembly)
                    {
                        StatusMessage = $"Error: {dbEx.Message}";
                        await Task.Delay(2000);
                        StatusMessage = string.Empty;
                    }
                    else
                    {
                        await _dialogService.ShowDialog(
                            $"Database error: {dbEx.Message}",
                            "Error",
                            new List<DialogButton> { new("OK", false) });
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during submission
                Console.WriteLine($"Error in Submit: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");

                ButtonText = "Submit";

                if (_isWebAssembly)
                {
                    StatusMessage = $"Error: {ex.Message}";
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                else
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
        }

        // Simplified cover search that works in WebAssembly
        private async Task<string?> SearchForCoverUrlAsync(string title)
        {
            try
            {
                if (_isWebAssembly)
                {
                    // Skip actual cover search in WebAssembly
                    _continueWithNullCover = true;
                    return null;
                }

                // Only try to get real covers in desktop mode
                return await GetBookCoverUrlAsync(title);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for cover: {ex.Message}");
                _continueWithNullCover = true;
                return null;
            }
        }

        // This method is only called in non-WebAssembly environments
        private async Task<string?> GetBookCoverUrlAsync(string title)
        {
            try
            {
                // This method would contain the OpenLibrary API call logic
                // Simplified placeholder for now
                Console.WriteLine("GetBookCoverUrlAsync - non-WebAssembly implementation would go here");

                // Let's simulate finding no cover for simplicity
                _continueWithNullCover = true;
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetBookCoverUrlAsync: {ex.Message}");
                _continueWithNullCover = true;
                return null;
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
            _continueWithNullCover = true;
        }
    }
}
