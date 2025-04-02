using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Models;
using Goweli.Services;
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
        private readonly IDatabaseService _databaseService;

        // Constructor for dependency injection
        public ViewBooksViewModel(MainViewModel mainViewModel, IDatabaseService databaseService)
        {
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            LoadBooks();
        }

        // Observable properties for data binding
        [ObservableProperty]
        private ObservableCollection<Book> _books = [];

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

        [ObservableProperty]
        private bool _isDeleteConfirmationVisible = false;

        private Book? _originalState;

        // Methods to handle property changes and update UI
        partial void OnStatusMessageChanged(string value)
        {
            IsStatusVisible = !string.IsNullOrEmpty(value);
        }

        // Method to watch for the user to select a book from the table
        partial void OnSelectedBookChanged(Book? value)
        {
            // When user selects a book, load the book cover from the URL stored in the database
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

        // Method to load the book cover of the book the user has selected
        private void LoadBookCover(string coverUrl)
        {
            try
            {
                Task.Run(async () =>
                {
                    try
                    {
                        await _mainViewModel.LoadBookCoverAsync(coverUrl); // The book cover is shown on the side menu, which is located in the Main View Model
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
                var books = await _databaseService.GetBooksAsync();
                Books = new ObservableCollection<Book>(books);
            }

            // If an error occurs, call method to load sample books
            catch (Exception)
            {
                StatusMessage = "Error loading books. Using sample data instead.";
                LoadSampleBooks();
            }

            // If no books have been entered, call method to load sample books
            if (!Books.Any())
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
                new() {
                    Id = 1,
                    BookTitle = "The Great Gatsby",
                    AuthorName = "F. Scott Fitzgerald",
                    ISBN = "9780743273565",
                    IsChecked = true,
                    Synopsis = "A story of wealth, love, and the American Dream in the 1920s.",
                    CoverUrl = "https://covers.openlibrary.org/b/olid/OL22570129M-M.jpg"
                },
                new() {
                    Id = 2,
                    BookTitle = "To Kill a Mockingbird",
                    AuthorName = "Harper Lee",
                    ISBN = "9780061120084",
                    IsChecked = true,
                    Synopsis = "A story about racial injustice and moral growth in the American South.",
                    CoverUrl = "https://covers.openlibrary.org/b/olid/OL37027359M-M.jpg"
                },
                new() {
                    Id = 3,
                    BookTitle = "1984",
                    AuthorName = "George Orwell",
                    ISBN = "9780451524935",
                    IsChecked = false,
                    Synopsis = "A dystopian novel about totalitarianism, surveillance, and thought control.",
                    CoverUrl = "https://covers.openlibrary.org/b/olid/OL21733390M-M.jpg"
                }
            };
            // Load the sample books into the database if none exist (can be deleted from the db later)
            foreach (var book in sampleBooks)
            {
                _databaseService.AddBookAsync(book).GetAwaiter().GetResult();
            }

            Books = sampleBooks;
        }

        // Method to show delete confirmation dialog
        [RelayCommand]
        private void ShowDeleteConfirmation()
        {
            IsDeleteConfirmationVisible = true;
        }

        // Method to confirm delete
        [RelayCommand]
        private async Task ConfirmDelete()
        {
            IsDeleteConfirmationVisible = false;
            await DeleteBook();
        }

        // Method to cancel delete
        [RelayCommand]
        private void CancelDelete()
        {
            IsDeleteConfirmationVisible = false;
        }

        // Method to delete a book
        private async Task DeleteBook()
        {
            try
            {
                // If the user has not selected a book, display a reminder
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
                    await _databaseService.DeleteBookAsync(bookToRemove);
                    Books.Remove(bookToRemove);
                    StatusMessage = $"'{bookToRemove.BookTitle} deleted";
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
                // Remind user to select a book before clicking edit button
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
                // Load selected book into text input fields so they can be edited
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
                IsEditing = true; // This is for updating the view model and is called in the axaml
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

                // Update the book using DatabaseService
                try
                {
                    // First delete the old book
                    await _databaseService.DeleteBookAsync(_originalState);

                    // Then add the updated book
                    await _databaseService.AddBookAsync(EditingBook);

                    StatusMessage = "Changes saved successfully";
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
            // Reset variables and UI states
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