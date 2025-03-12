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

        // React to changes in SelectedBook to display book cover
        partial void OnSelectedBookChanged(Book? value)
        {
            Console.WriteLine($"SelectedBook changed: {value?.BookTitle ?? "null"}");

            if (value != null)
            {
                // Load book cover if available
                if (!string.IsNullOrEmpty(value.CoverUrl))
                {
                    Console.WriteLine($"Loading cover URL: {value.CoverUrl}");
                    LoadBookCover(value.CoverUrl);
                }
                else
                {
                    Console.WriteLine("No cover URL available, clearing cover");
                    _mainViewModel.ClearBookCover();
                }
            }
            else
            {
                Console.WriteLine("Selected book is null, clearing cover");
                _mainViewModel.ClearBookCover();
            }
        }

        // Helper method to load book cover
        private void LoadBookCover(string coverUrl)
        {
            try
            {
                Console.WriteLine($"Loading book cover from URL: {coverUrl}");

                // Use Task.Run to avoid blocking UI thread, but don't await it
                Task.Run(async () =>
                {
                    try
                    {
                        await _mainViewModel.LoadBookCoverAsync(coverUrl);
                        Console.WriteLine("Book cover loaded successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in async book cover loading: {ex.Message}");
                        _mainViewModel.ClearBookCover();
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initiating book cover load: {ex.Message}");
                _mainViewModel.ClearBookCover();
            }
        }

        public ViewBooksViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            Console.WriteLine("ViewBooksViewModel created");

            // Load books from the static collection
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

                Console.WriteLine($"Loaded {Books.Count} books");
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
            // Create some sample books
            var sampleBooks = new ObservableCollection<Book>
    {
        new Book
        {
            Id = 1,
            BookTitle = "The Great Gatsby",
            AuthorName = "F. Scott Fitzgerald",
            ISBN = "9780743273565",
            IsChecked = true,
            Synopsis = "A story of wealth, love, and the American Dream in the 1920s.",
            CoverUrl = "https://covers.openlibrary.org/b/olid/OL22570129M-M.jpg"
        },
        new Book
        {
            Id = 2,
            BookTitle = "To Kill a Mockingbird",
            AuthorName = "Harper Lee",
            ISBN = "9780061120084",
            IsChecked = true,
            Synopsis = "A story about racial injustice and moral growth in the American South.",
            CoverUrl = "https://covers.openlibrary.org/b/olid/OL37027359M-M.jpg"
        },
        new Book
        {
            Id = 3,
            BookTitle = "1984",
            AuthorName = "George Orwell",
            ISBN = "9780451524935",
            IsChecked = false,
            Synopsis = "A dystopian novel about totalitarianism, surveillance, and thought control.",
            CoverUrl = "https://covers.openlibrary.org/b/olid/OL21733390M-M.jpg"
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
        private async Task DeleteBook()
        {
            if (SelectedBook == null)
            {
                StatusMessage = "Please select a book";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
                return;
            }

            StatusMessage = $"Deleting '{SelectedBook.BookTitle}'";
            await Task.Delay(1000);

            // Remove from in-memory collection
            var bookTitle = SelectedBook.BookTitle;
            Books.Remove(SelectedBook);

            // Clear the book cover if it's currently displayed
            _mainViewModel.ClearBookCover();

            SelectedBook = null;

            StatusMessage = $"'{bookTitle}' deleted";
            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }
    }
}