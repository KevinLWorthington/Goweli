using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Goweli.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using System.Net.Http;
using System.Diagnostics;

// View model for viewing books stored in the database

namespace Goweli.ViewModels
{
    public partial class ViewBooksViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IDialogService _dialogService;

        // Collection of books to be displayed

        public ObservableCollection<Book> Books { get; }
        
        // Allows selection of a book so the user can delete it

        [ObservableProperty]
        private Book? _selectedBook;

        [ObservableProperty]
        private Bitmap? _coverImage;

        [ObservableProperty]
        private bool _isCoverVisible;

        public RelayCommand DeleteCommand { get; }
        public RelayCommand<Book> BookDoubleClickCommand { get; }

        // Sets up and populates the books from the database and sets up the delete command

        public ViewBooksViewModel(MainViewModel mainViewModel, IDialogService dialogService)
        {
            _mainViewModel = mainViewModel;
            _dialogService = dialogService;

            using var db = new AppDbContext();
            var books = db.Books.ToList();
            Books = new ObservableCollection<Book>(books);

            DeleteCommand = new RelayCommand(async () => await DeleteSelectedBookAsync(), CanDeleteBook);
            BookDoubleClickCommand = new RelayCommand<Book>(async (book) => await OnBookDoubleClickAsync(book));
        }


        
        partial void OnSelectedBookChanged(Book? value)
        {
            DeleteCommand.NotifyCanExecuteChanged();
        }

        private bool CanDeleteBook()
        {
            return SelectedBook != null;
        }

        // Prompts the user to confirm deletion of a book and deletes it if confirmed

        private async Task DeleteSelectedBookAsync()
        {
            if (SelectedBook != null)
            {
                bool isConfirmed = await _dialogService.ShowConfirmationDialog(
                    $"Are you sure you want to delete '{SelectedBook.BookTitle}'?",
                    "Confirm Deletion");

                if (isConfirmed)
                {
                    using var db = new AppDbContext();
                    var bookToDelete = db.Books.Find(SelectedBook.Id);
                    if (bookToDelete != null)
                    {
                        db.Books.Remove(bookToDelete);
                        db.SaveChanges();
                        Books.Remove(SelectedBook);
                    }
                }
            }
        }
        private async Task OnBookDoubleClickAsync(Book book)
        {
            if (SelectedBook != null && !string.IsNullOrEmpty(book.CoverUrl))
            {
                CoverImage = await LoadImageFromUrl(book.CoverUrl);
                IsCoverVisible = CoverImage != null;
            }
            else
            {
                IsCoverVisible = false;
            }
        }

        // Loads an image from a URL
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
                Debug.WriteLine($"Error loading image: {ex.Message}");
                return null;
            }
        }
    }
}
