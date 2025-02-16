using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Goweli.Services;
using System;
using System.Collections.Generic;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DialogService _dialogService;

        public AddBookViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _dialogService = new DialogService();
            SubmitCommand = new RelayCommand(OnSubmit);
        }

        [ObservableProperty]
        private string _bookTitle = string.Empty;

        [ObservableProperty]
        private string _authorName = string.Empty;

        [ObservableProperty]
        private string? _iSBN = string.Empty;

        [ObservableProperty]
        private string? _synopsis = string.Empty;

        [ObservableProperty]
        private bool _isChecked;

        public RelayCommand SubmitCommand { get; }

        private async void OnSubmit()
        {
            // Check if the Author and Title are empty and prompt user if so
            if (string.IsNullOrWhiteSpace(BookTitle) || string.IsNullOrWhiteSpace(AuthorName))
            {
                var buttons = new List<DialogButton>
        {
            new("OK", false)
        };

                await _dialogService.ShowDialog(
                    "Author and Title Required.",
                    "ERROR",
                    buttons);
                return;
            }

            // Create a new Book instance
            var newBook = new Book
            {
                BookTitle = this.BookTitle,
                AuthorName = this.AuthorName,
                ISBN = this.ISBN,
                Synopsis = this.Synopsis,
                IsChecked = this.IsChecked
            };

            try
            {
                // Save to the database
                using var db = new AppDbContext();
                db.Books.Add(newBook);
                db.SaveChanges();

                // Navigate back to the starting screen
                _mainViewModel.ShowDefaultView();
            }
            catch (Exception)
            {
                var buttons = new List<DialogButton>
        {
            new("OK", false)
        };
            }

        }
    }
}
