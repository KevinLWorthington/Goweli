using Goweli.Data;
using Goweli.Models;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Goweli.ViewModels
{
    public partial class ViewBooksViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        public ObservableCollection<Book> Books { get; }

        [ObservableProperty]
        private Book? _selectedBook;

        public RelayCommand DeleteCommand { get; }

        public ViewBooksViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;

            using var db = new AppDbContext();
            var books = db.Books.ToList();
            Books = new ObservableCollection<Book>(books);

            DeleteCommand = new RelayCommand(DeleteSelectedBook, CanDeleteBook);
        }

        partial void OnSelectedBookChanged(Book? value)
        {
            DeleteCommand.NotifyCanExecuteChanged();
        }

        private bool CanDeleteBook()
        {
            return SelectedBook != null;
        }

        private void DeleteSelectedBook()
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
        }
    }
}
