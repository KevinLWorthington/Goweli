using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Goweli.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class ViewBooksViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly IDialogService _dialogService;

        public ObservableCollection<Book> Books { get; }

        [ObservableProperty]
        private Book? _selectedBook;

        public RelayCommand DeleteCommand { get; }

        public ViewBooksViewModel(MainViewModel mainViewModel, IDialogService dialogService)
        {
            _mainViewModel = mainViewModel;
            _dialogService = dialogService;

            using var db = new AppDbContext();
            var books = db.Books.ToList();
            Books = new ObservableCollection<Book>(books);

            // DeleteCommand = new RelayCommand(DeleteSelectedBook, CanDeleteBook);

            DeleteCommand = new RelayCommand(async () => await DeleteSelectedBookAsync(), CanDeleteBook);
        }

        partial void OnSelectedBookChanged(Book? value)
        {
            DeleteCommand.NotifyCanExecuteChanged();
        }

        private bool CanDeleteBook()
        {
            return SelectedBook != null;
        }

        /* private void DeleteSelectedBook()
         {
             if (SelectedBook != null)
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
         } */
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
    }
}
