using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Goweli.ViewModels
{
    public partial class AddBookViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainViewModel;

        public AddBookViewModel(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            SubmitCommand = new RelayCommand(OnSubmit);
        }

        [ObservableProperty]
        private string _bookTitle = string.Empty;

        [ObservableProperty]
        private string _authorName = string.Empty;

        [ObservableProperty]
        private string? _iSBN;

        [ObservableProperty]
        private string _synopsis = string.Empty;

        [ObservableProperty]
        private bool _isChecked;

       /* public AddBookViewModel()
        {
            SubmitCommand = new RelayCommand(OnSubmit);
        } */

        public RelayCommand SubmitCommand { get; }

        private void OnSubmit()
        { 

            // Create a new Book instance
            var newBook = new Book
            {
                BookTitle = this.BookTitle,
                AuthorName = this.AuthorName,
                ISBN = this.ISBN,
                Synopsis = this.Synopsis,
                IsChecked = this.IsChecked
            };

            // Save to the database
            using var db = new AppDbContext();
            db.Books.Add(newBook);
            db.SaveChanges();

            // Navigate back to the starting screen
            _mainViewModel.ShowDefaultView();
        }
        
    }
}
