using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class SearchViewModel : ViewModelBase
    {
        // Private fields for dependency injection
        private readonly MainViewModel _mainViewModel;
        private readonly GoweliDbContext _dbContext;

        // Search types
        public List<string> SearchTypes { get; } = new List<string> { "Title", "Author", "ISBN" };

        // Observable properties for data binding
        [ObservableProperty]
        private ObservableCollection<Book> _searchResults = new();

        [ObservableProperty]
        private Book? _selectedBook;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _searchStatus = string.Empty;

        [ObservableProperty]
        private bool _isSearching = false;

        [ObservableProperty]
        private string _selectedSearchType = "Title";

        // Constructor for dependency injection
        public SearchViewModel(MainViewModel mainViewModel, GoweliDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
        }

        // Method to watch for the user to select a book from the results
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
                        await _mainViewModel.LoadBookCoverAsync(coverUrl);
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

        // Method to search books based on user input
        [RelayCommand]
        private async Task Search()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                SearchStatus = "Please enter search text";
                await Task.Delay(2000);
                SearchStatus = string.Empty;
                return;
            }

            try
            {
                IsSearching = true;
                SearchStatus = "Searching...";
                List<Book> results = new List<Book>();

                // Search based on selected search type
                // Get all books first and then filter in-memory
                var allBooks = await _dbContext.Books.ToListAsync();

                // Case-insensitive search using string operations in memory
                switch (SelectedSearchType)
                {
                    case "Title":
                        results = allBooks
                            .Where(b => b.BookTitle.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToList();
                        break;
                    case "Author":
                        results = allBooks
                            .Where(b => b.AuthorName.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToList();
                        break;
                    case "ISBN":
                        results = allBooks
                            .Where(b => b.ISBN != null && b.ISBN.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0)
                            .ToList();
                        break;
                }

                SearchResults = new ObservableCollection<Book>(results);

                if (SearchResults.Count == 0)
                {
                    SearchStatus = "No books found matching your search criteria";
                }
                else
                {
                    SearchStatus = $"Found {SearchResults.Count} book(s)";
                }
            }
            catch (Exception ex)
            {
                SearchStatus = $"Error during search: {ex.Message}";
                Console.WriteLine($"Error during search: {ex.Message}");
            }
            finally
            {
                IsSearching = false;
                await Task.Delay(2000);
                SearchStatus = string.Empty;
            }
        }

        // Method to clear search results
        [RelayCommand]
        private void ClearSearch()
        {
            SearchText = string.Empty;
            SearchResults.Clear();
            SelectedBook = null;
            _mainViewModel.ClearBookCover();
            SearchStatus = string.Empty;
        }
    }
}