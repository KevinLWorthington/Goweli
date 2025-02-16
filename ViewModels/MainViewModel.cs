using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Services;

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
        public RelayCommand ShowDefaultViewCommand { get; }

        private readonly IDialogService _dialogService;

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;

            AddBookViewCommand = new RelayCommand(ShowAddBookView);
            ViewBooksCommand = new RelayCommand(ShowViewBooks);
            ShowDefaultViewCommand = new RelayCommand(ShowDefaultView);
            // SearchCommand = new RelayCommand(ShowSearchView); // If Search is implemented

            // Set an initial view
            ShowDefaultView();

        }
        // Sets which view model should be shown
        private void ShowAddBookView()
        {
            CurrentViewModel = new AddBookViewModel(this);
        }

        private void ShowViewBooks()
        {
            CurrentViewModel = new ViewBooksViewModel(this, _dialogService);
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
