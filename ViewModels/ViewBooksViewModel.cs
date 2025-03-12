using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
using Goweli.Data;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

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

        private async void LoadBooks()
        {
            try
            {
                using (var context = new AppDbContext())
                {
                    var books = await context.Books.ToListAsync();
                    Books = new ObservableCollection<Book>(books);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading books: {ex.Message}");
                StatusMessage = "Error loading books.";
            }
        }
        [RelayCommand]
        private async Task ViewBookDetails()
        {
            if (SelectedBook == null)
            {
                StatusMessage = "Please select a book";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
                return;
            }

            if (!string.IsNullOrEmpty(SelectedBook.CoverUrl))
            {
                await _mainViewModel.LoadBookCoverAsync(SelectedBook.CoverUrl);
            }

            StatusMessage = $"Viewing details for '{SelectedBook.BookTitle}";
            await Task.Delay(2000);
            StatusMessage = string.Empty;
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
            await Task.Delay(2000);

            try
            {
                using (var context = new AppDbContext())
                {
                    context.Books.Remove(SelectedBook);
                    await context.SaveChangesAsync();
                }

                Books.Remove(SelectedBook);
                SelectedBook = null;

                StatusMessage = $"{SelectedBook.BookTitle} deleted";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting book: {ex.Message}");
                StatusMessage = "Error deleting book.";
            }
        }

       /* private void LoadBooks()
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
        } */

            // Display book details or navigate to details page
            // For now, just show the cover if available
            

        

        [RelayCommand]
        private void ToggleRead()
        {
            if (SelectedBook == null) return;

            // Toggle the read status
            SelectedBook.IsChecked = !SelectedBook.IsChecked;
            StatusMessage = SelectedBook.IsChecked
                ? $"Marked '{SelectedBook.BookTitle}' as read"
                : $"Marked '{SelectedBook.BookTitle}' as unread";

            try
            {
                using (var context = new AppDbContext())
                {
                    context.Books.Update(SelectedBook);
                    context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating book: {ex.Message}");
                StatusMessage = "Error updating book.";
            }

            var index = Books.IndexOf(SelectedBook);
            if (index != -1)
            {
                Books[index] = SelectedBook;
            }
        }
    }
}