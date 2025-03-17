using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using OpenLibraryNET.Utility;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class ViewBooksViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly GoweliDbContext _dbContext;

        public ViewBooksViewModel(MainViewModel mainViewModel, GoweliDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(MainViewModel));

            LoadBooks();
        }

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

        private async Task LoadBooks()
        {
            try
            {
                var books = await _dbContext.Books.ToListAsync();
                Books = new ObservableCollection<Book>(books);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading books: {ex.Message}");
                StatusMessage = "Error loading books. Using sample data instead.";
                LoadSampleBooks();
            }

            if (Books.Count == 0)
            {
                StatusMessage = "No books found. Using sample data instead.";
                LoadSampleBooks();
            }
        }

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
                Console.WriteLine($"Attempting to delete book: {SelectedBook.Id} - {SelectedBook.BookTitle}");

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
                        Console.WriteLine("Book deleted successfully from database");
                        StatusMessage = $"'{bookToRemove.BookTitle} deleted";
                    }
                    else
                    {
                        Console.WriteLine("Database operation timed out");
                        StatusMessage = "Operation timed out, updating UI only";

                    }

                    Books.Remove(bookToRemove);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting book: {ex.Message}");
                    StatusMessage = $"Error: {ex.Message}";
                }

                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Unhandled error in DeleteBook: {ex.Message}");
                StatusMessage = "An error occurred";
                await Task.Delay(2000);
                StatusMessage = string.Empty;
            }
        }

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

                Console.WriteLine($"Starting edit for book: {SelectedBook.Id} - {SelectedBook.BookTitle}");
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
                Console.WriteLine($"Error starting edit: {ex.Message}");
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
                Console.WriteLine($"Saving changes to book: {EditingBook.Id} - {EditingBook.BookTitle}");

                // First update the UI to maintain responsiveness
                int index = Books.IndexOf(_originalState);
                if (index >= 0)
                {
                    Books[index] = EditingBook;
                }

                // Update selected book
                SelectedBook = EditingBook;

                try
                {
                    // Update in database with timeout protection
                    var entity = await _dbContext.Books.FindAsync(EditingBook.Id);
                    if (entity != null)
                    {
                        // Update all properties
                        entity.BookTitle = EditingBook.BookTitle;
                        entity.AuthorName = EditingBook.AuthorName;
                        entity.ISBN = EditingBook.ISBN;
                        entity.IsChecked = EditingBook.IsChecked;
                        entity.Synopsis = EditingBook.Synopsis;
                        entity.CoverUrl = EditingBook.CoverUrl;

                        // Save with timeout
                        var saveTask = _dbContext.SaveChangesAsync();
                        var completedTask = await Task.WhenAny(saveTask, Task.Delay(5000));

                        if (completedTask == saveTask)
                        {
                            // The save completed normally
                            Console.WriteLine("Book updated successfully in database");
                            StatusMessage = "Changes saved successfully";
                        }
                        else
                        {
                            // The save timed out
                            Console.WriteLine("Database update operation timed out");
                            StatusMessage = "Database operation timed out, but UI updated";
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Book with ID {EditingBook.Id} not found in database");
                        StatusMessage = "Book not found in database, but UI updated";
                    }
                }
                catch (Exception dbEx)
                {
                    // If database update fails, at least the UI is updated
                    Console.WriteLine($"Database error: {dbEx.Message}");
                    StatusMessage = "Changes saved to UI only (database error)";
                }

                IsEditing = false;
                EditingBook = null;
                _originalState = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving book: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                StatusMessage = $"Error saving changes: {ex.Message}";
            }

            await Task.Delay(2000);
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private void CancelEdit()
        {
            try
            {
                Console.WriteLine("Canceling edit");
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error canceling edit: {ex.Message}");
            }
        }
    }
}