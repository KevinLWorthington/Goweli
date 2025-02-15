using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Goweli.ViewModels
{
    public partial class MainViewModel : ViewModelBase
    {
        [ObservableProperty]
        private object _currentViewModel;

        // Commands for left side buttons
        public RelayCommand AddBookViewCommand { get; }
        public RelayCommand ViewBooksCommand { get; }
        public RelayCommand SearchCommand { get; }

        public MainViewModel()
        {
            AddBookViewCommand = new RelayCommand(ShowAddBookView);
           // ViewBooksCommand = new RelayCommand(ShowViewBooks);
           // SearchCommand = new RelayCommand(ShowSearchView); // If Search is implemented

            // Set an initial view
            // CurrentViewModel = new HomeViewModel();
            ShowDefaultView();
        }
        // Sets which view model should be shown
        private void ShowAddBookView()
        {
            CurrentViewModel = new AddBookViewModel(this);
        }

        private void ShowViewBooks()
        {
            CurrentViewModel = new ViewBooksViewModel();
        }

        private void ShowSearchView()
        {
            CurrentViewModel = new SearchViewModel();
        }

        public void ShowDefaultView()
        {
            CurrentViewModel = new HomeViewModel();
        }
    }
}
