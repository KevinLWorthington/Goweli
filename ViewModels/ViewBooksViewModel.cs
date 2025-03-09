using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class ViewBooksViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private ObservableCollection<Book> _books = new();

        [ObservableProperty]
        private Book? _selectedBook;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusVisible = false;

        // When StatusMessage is set, also update IsStatusVisible
        partial void OnStatusMessageChanged(string value)
        {
            IsStatusVisible = !string.IsNullOrEmpty(value);
        }

        public ViewBooksViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // Load books from the static collection in AddBookViewModel
            LoadBooks();
        }

        private void LoadBooks()
        {
            try
            {
                // If we have no books in the static collection, add some sample ones
                if (AddBookViewModel.Books.Count == 0)
                {
                    LoadSampleBooks();
                    StatusMessage = "Showing sample books";
                }
                else
                {
                    Books = AddBookViewModel.Books;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading books: {ex.Message}");
                StatusMessage = "Error loading books. Using sample data instead.";
                LoadSampleBooks();
            }
        }

        private void LoadSampleBooks()
        {
            // Create some sample books for the WebAssembly demo
            var sampleBooks = new ObservableCollection<Book>
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

            // Add the sample books to our static collection if it's empty
            if (AddBookViewModel.Books.Count == 0)
            {
                foreach (var book in sampleBooks)
                {
                    AddBookViewModel.Books.Add(book);
                }
            }

            Books = AddBookViewModel.Books;
        }

        [RelayCommand]
        private async Task ViewBookDetails()
        {
            if (SelectedBook == null)
            {
                StatusMessage = "Please select a book first";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
                return;
            }

            // Display book details or navigate to details page
            // For now, just show the cover if available
            if (!string.IsNullOrEmpty(SelectedBook.CoverUrl))
            {
                await _mainViewModel.LoadBookCoverAsync(SelectedBook.CoverUrl);
            }

            StatusMessage = $"Viewing details for: {SelectedBook.BookTitle}";
            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private async Task DeleteBook()
        {
            if (SelectedBook == null)
            {
                StatusMessage = "Please select a book first";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
                return;
            }

            // Simple confirmation in the UI
            StatusMessage = $"Deleting book: {SelectedBook.BookTitle}";
            await Task.Delay(1000);

            // Remove from shared collection
            AddBookViewModel.Books.Remove(SelectedBook);
            Books = AddBookViewModel.Books;
            SelectedBook = null;

            StatusMessage = "Book deleted successfully";
            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private void ToggleRead()
        {
            if (SelectedBook == null) return;

            // Toggle the read status
            SelectedBook.IsChecked = !SelectedBook.IsChecked;
            StatusMessage = SelectedBook.IsChecked
                ? $"Marked '{SelectedBook.BookTitle}' as read"
                : $"Marked '{SelectedBook.BookTitle}' as unread";

            // Force update the collection
            var index = Books.IndexOf(SelectedBook);
            if (index >= 0)
            {
                Books[index] = SelectedBook;
            }
        }
    }
}