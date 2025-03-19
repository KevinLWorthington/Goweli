using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class ViewBooksViewModel : ViewModelBase
    {
        // Private fields for dependency injection
        private readonly MainViewModel _mainViewModel;
        private readonly GoweliDbContext _dbContext;

        // Constructor for dependency injection
        public ViewBooksViewModel(MainViewModel mainViewModel, GoweliDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(MainViewModel));

            LoadBooks();
        }

        // Observable properties for data binding
        [ObservableProperty]
        private ObservableCollection<Book> _books = new();

        [ObservableProperty]
        private Book? _selectedBook;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isStatusVisible = false;

        [ObservableProperty]
        private Book? _editingBook;

        [ObservableProperty]
        private bool _isEditing = false;

        private Book? _originalState;

        // Methods to handle property changes and update UI
        partial void OnStatusMessageChanged(string value)
        {
            IsStatusVisible = !string.IsNullOrEmpty(value);
        }

        partial void OnSelectedBookChanged(Book? value)
        {

            if (value != null)
            {
                if (!string.IsNullOrEmpty(value.CoverUrl))
                {
                    LoadBookCover(value.CoverUrl);
                }
                else
                {
                    _mainViewModel.ClearBookCover();
                }
            }
            else
            {
                _mainViewModel.ClearBookCover();
            }
        }

        // Method to load the book cover
        private void LoadBookCover(string coverUrl)
        {
            try
            {

                Task.Run(async () =>
                {
                    try
                    {
                        await _mainViewModel.LoadBookCoverAsync(coverUrl);
                    }
                    catch (Exception)
                    {
                        _mainViewModel.ClearBookCover();
                    }
                });
            }
            catch (Exception)
            {
                _mainViewModel.ClearBookCover();
            }
        }

        // Method to load the books from the database
        private async Task LoadBooks()
        {
            try
            {
                var books = await _dbContext.Books.ToListAsync();
                Books = new ObservableCollection<Book>(books);
            }
            catch (Exception)
            {
                StatusMessage = "Error loading books. Using sample data instead.";
                LoadSampleBooks();
            }

            if (Books.Count == 0)
            {
                StatusMessage = "No books found. Using sample data instead.";
                LoadSampleBooks();
            }
        }

        // Method to load sample books if there are no books in the database
        private void LoadSampleBooks()
        {
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

            if (_dbContext.Books.Count() == 0)
            {
                foreach (var book in sampleBooks)
                {
                    _dbContext.Books.Add(book);
                }
                _dbContext.SaveChanges();
            }

            Books = new ObservableCollection<Book>(sampleBooks);
        }

        // Method to delete a book
        [RelayCommand]
        private async Task DeleteBook()
        {
            try
            {
                if (SelectedBook == null)
                {
                    StatusMessage = "Please select a book";
                    await Task.Delay(2000);
                    StatusMessage = string.Empty;
                    return;
                }

                StatusMessage = $"Deleting '{SelectedBook.BookTitle}'";

                var bookToRemove = SelectedBook;

                SelectedBook = null;

                _mainViewModel.ClearBookCover();

                try
                {
                    _dbContext.Books.Remove(bookToRemove);

                    var saveTask = _dbContext.SaveChangesAsync();
                    var completedTask = await Task.WhenAny(saveTask, Task.Delay(5000));

                    if (completedTask == saveTask)
                    {
                        StatusMessage = $"'{bookToRemove.BookTitle} deleted";
                    }
                    else
                    {
                        StatusMessage = "Operation timed out, updating UI only";

                    }

                    Books.Remove(bookToRemove);
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error: {ex.Message}";
                }

                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            catch (Exception)
            {
                StatusMessage = "An error occurred";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
        }

        // Method to edit a book
        [RelayCommand]
        private void EditBook()
        {
            try
            {
                if (SelectedBook == null)
                {
                    StatusMessage = "Please select a book";
                    Task.Run(async () =>
                    {
                        await Task.Delay(2000);
                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            StatusMessage = string.Empty;
                        });
                    });
                    return;
                }

                EditingBook = new Book
                {
                    Id = SelectedBook.Id,
                    BookTitle = SelectedBook.BookTitle,
                    AuthorName = SelectedBook.AuthorName,
                    ISBN = SelectedBook.ISBN,
                    IsChecked = SelectedBook.IsChecked,
                    Synopsis = SelectedBook.Synopsis,
                    CoverUrl = SelectedBook.CoverUrl
                };

                _originalState = SelectedBook;
                IsEditing = true;
                StatusMessage = $"Editing '{SelectedBook.BookTitle}'";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage = string.Empty;
                    });
                });
            }
        }

        // Method to save changes to a book
        [RelayCommand]
        private async Task SaveEdit()
        {
            if (EditingBook == null || _originalState == null)
            {
                StatusMessage = "No book to save";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
                return;
            }

            try
            {
                StatusMessage = "Saving changes...";

                int index = Books.IndexOf(_originalState);
                if (index >= 0)
                {
                    Books[index] = EditingBook;
                }

                SelectedBook = EditingBook;

                try
                {
                    var entity = await _dbContext.Books.FindAsync(EditingBook.Id);
                    if (entity != null)
                    {
                        entity.BookTitle = EditingBook.BookTitle;
                        entity.AuthorName = EditingBook.AuthorName;
                        entity.ISBN = EditingBook.ISBN;
                        entity.IsChecked = EditingBook.IsChecked;
                        entity.Synopsis = EditingBook.Synopsis;
                        entity.CoverUrl = EditingBook.CoverUrl;

                        var saveTask = _dbContext.SaveChangesAsync();
                        var completedTask = await Task.WhenAny(saveTask, Task.Delay(5000));

                        if (completedTask == saveTask)
                        {
                            StatusMessage = "Changes saved successfully";
                        }
                        else
                        {
                            StatusMessage = "Database operation timed out";
                        }
                    }
                    else
                    {
                        StatusMessage = "Book not found in database, but UI updated";
                    }
                }
                catch (Exception)
                {
                    StatusMessage = "Database error";
                }

                IsEditing = false;
                EditingBook = null;
                _originalState = null;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving changes: {ex.Message}";
            }

            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }

        // Method to cancel book editing
        [RelayCommand]
        private void CancelEdit()
        {
                EditingBook = null;
                _originalState = null;
                IsEditing = false;
                StatusMessage = "Edit canceled";

                Task.Run(async () =>
                {
                    await Task.Delay(2000);
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        StatusMessage = string.Empty;
                    });
                });
        }
    }
}