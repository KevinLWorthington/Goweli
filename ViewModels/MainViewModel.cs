using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Services;
using System.Diagnostics;
using System;
using System.IO;
using System.Net.Http;

namespace Goweli.ViewModels
{

    // Sets up commands for view switching

    public partial class MainViewModel : ViewModelBase
    {
        // Current view model to be shown

        [ObservableProperty]
        private object _currentViewModel;

        [ObservableProperty]
        private Bitmap? _bookCoverImage;

        [ObservableProperty]
        private bool _isBookCoverVisible;

        // Commands for left side buttons
        public RelayCommand AddBookViewCommand { get; }
        public RelayCommand ViewBooksCommand { get; }
        public RelayCommand SearchCommand { get; }
        public RelayCommand ShowDefaultViewCommand { get; }

        private readonly IDialogService _dialogService;

        public MainViewModel(IDialogService dialogService)
        {
            _dialogService = dialogService;
            // Links commands to the view models
            AddBookViewCommand = new RelayCommand(ShowAddBookView);
            ViewBooksCommand = new RelayCommand(ShowViewBooks);
            ShowDefaultViewCommand = new RelayCommand(ShowDefaultView);
            // SearchCommand = new RelayCommand(ShowSearchView); // If Search is implemented

            // Set an initial view
            ShowDefaultView();

        }

        public async void LoadBookCover(string coverUrl)
        {
            try
            {
                using var httpClient = new HttpClient();
                var imageBytes = await httpClient.GetByteArrayAsync(coverUrl);

                using var memoryStream = new MemoryStream(imageBytes);
                BookCoverImage = new Bitmap(memoryStream);
                IsBookCoverVisible = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading image: {ex.Message}");
                ClearBookCover();
            }
        }

        public void ClearBookCover()
        {
            BookCoverImage = null;
            IsBookCoverVisible = false;
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
