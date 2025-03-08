using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Goweli.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class ViewBooksViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DialogService _dialogService;
        private readonly bool _isWebAssembly;

        [ObservableProperty]
        private ObservableCollection<Book> _books = new();

        [ObservableProperty]
        private Book? _selectedBook;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        public ViewBooksViewModel(MainViewModel mainViewModel, DialogService dialogService, bool isWebAssembly = false)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _isWebAssembly = isWebAssembly;

            // Load books when the view model is created
            LoadBooks();
        }

        private void LoadBooks()
        {
            try
            {
                if (_isWebAssembly)
                {
                    // In WebAssembly mode, use sample data instead of database access
                    LoadSampleBooks();
                    StatusMessage = "Showing sample books (WebAssembly Demo Mode)";
                }
                else
                {
                    // In desktop mode, load from the database
                    using var db = new AppDbContext();
                    var booksList = db.Books.ToList();
                    Books = new ObservableCollection<Book>(booksList);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading books: {ex.Message}");

                if (_isWebAssembly)
                {
                    StatusMessage = "Error loading books. Using sample data instead.";
                    LoadSampleBooks();
                }
            }
        }

        private void LoadSampleBooks()
        {
            // Create some sample books for the WebAssembly demo
            Books = new ObservableCollection<Book>
            {
                new Book
                {
                    Id = 1,
                    BookTitle = "The Great Gatsby",
                    AuthorName = "F. Scott Fitzgerald",
                    ISBN = "9780743273565",
                    IsChecked = true,
                    Synopsis = "A story of wealth, love, and the American Dream in the 1920s."
                },
                new Book
                {
                    Id = 2,
                    BookTitle = "To Kill a Mockingbird",
                    AuthorName = "Harper Lee",
                    ISBN = "9780061120084",
                    IsChecked = true,
                    Synopsis = "A story about racial injustice and moral growth in the American South."
                },
                new Book
                {
                    Id = 3,
                    BookTitle = "1984",
                    AuthorName = "George Orwell",
                    ISBN = "9780451524935",
                    IsChecked = false,
                    Synopsis = "A dystopian novel about totalitarianism, surveillance, and thought control."
                }
            };
        }

        [RelayCommand]
        private async Task ViewBookDetails()
        {
            if (SelectedBook == null)
            {
                if (_isWebAssembly)
                {
                    StatusMessage = "Please select a book first";
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                else
                {
                    await _dialogService.ShowDialog(
                        "Please select a book first.",
                        "No Book Selected",
                        new List<DialogButton> { new("OK", false) });
                }
                return;
            }

            // Display book details or navigate to details page
            // For now, just show the cover if available
            if (!string.IsNullOrEmpty(SelectedBook.CoverUrl))
            {
                await _mainViewModel.LoadBookCoverAsync(SelectedBook.CoverUrl);
            }

            if (_isWebAssembly)
            {
                StatusMessage = $"Viewing details for: {SelectedBook.BookTitle}";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
        }

        [RelayCommand]
        private async Task DeleteBook()
        {
            if (SelectedBook == null)
            {
                if (_isWebAssembly)
                {
                    StatusMessage = "Please select a book first";
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                }
                else
                {
                    await _dialogService.ShowDialog(
                        "Please select a book first.",
                        "No Book Selected",
                        new List<DialogButton> { new("OK", false) });
                }
                return;
            }

            if (_isWebAssembly)
            {
                // In WebAssembly, just simulate deletion
                StatusMessage = $"Deleting book: {SelectedBook.BookTitle}";
                await Task.Delay(1000);

                Books.Remove(SelectedBook);
                SelectedBook = null;

                StatusMessage = "Book deleted successfully (WebAssembly Demo Mode)";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            else
            {
                // In desktop mode, confirm with dialog and use database
                var dialogResult = await _dialogService.ShowDialog(
                    $"Are you sure you want to delete '{SelectedBook.BookTitle}'?",
                    "Confirm Delete",
                    new List<DialogButton> { new("Yes", true), new("No", false) });

                // Check if user confirmed the deletion
                if (dialogResult.WasConfirmed)
                {
                    using var db = new AppDbContext();
                    var book = db.Books.Find(SelectedBook.Id);
                    if (book != null)
                    {
                        db.Books.Remove(book);
                        await db.SaveChangesAsync();

                        Books.Remove(SelectedBook);
                        SelectedBook = null;
                    }
                }
            }
        }

        [RelayCommand]
        private void ToggleRead()
        {
            if (SelectedBook == null) return;

            if (_isWebAssembly)
            {
                // In WebAssembly, just update the UI model
                SelectedBook.IsChecked = !SelectedBook.IsChecked;
                StatusMessage = SelectedBook.IsChecked
                    ? $"Marked '{SelectedBook.BookTitle}' as read"
                    : $"Marked '{SelectedBook.BookTitle}' as unread";

                // Force update the ObservableCollection
                var index = Books.IndexOf(SelectedBook);
                if (index >= 0)
                {
                    Books[index] = SelectedBook;
                }
            }
            else
            {
                // In desktop mode, update in database
                using var db = new AppDbContext();
                var book = db.Books.Find(SelectedBook.Id);
                if (book != null)
                {
                    book.IsChecked = !book.IsChecked;
                    db.SaveChanges();

                    // Update the UI model to match
                    SelectedBook.IsChecked = book.IsChecked;

                    // Force update the ObservableCollection
                    var index = Books.IndexOf(SelectedBook);
                    if (index >= 0)
                    {
                        Books[index] = SelectedBook;
                    }
                }
            }
        }
    }
}