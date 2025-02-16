using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Goweli.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Goweli.ViewModels
{
    // Class for adding a book to the database

    public partial class AddBookViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;
        private readonly DialogService _dialogService;

        // Sets up the main view, the dialog window view, and the submit command

        public AddBookViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            _dialogService = new DialogService();
            SubmitCommand = new RelayCommand(OnSubmit);
            ButtonText = "Submit";
        }

        // Properties for the book to be added

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

        // Initial text for the submit button

        [ObservableProperty]
        private string _buttonText = string.Empty;

        public RelayCommand SubmitCommand { get; }

        private async void OnSubmit()
        {
            // Check if the Author and Title are empty and prompt user to go back if so
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

                // Change the button text to show that the book was added and add delay so the user can see it
                ButtonText = "Book Added!";
                await Task.Delay(2000);

                // Navigate back to the starting screen
                _mainViewModel.ShowDefaultView();
            }
            catch (Exception)
            {
                _ = new List<DialogButton>
            {
                new("OK", false)
            };
            }

        }
    }
}
