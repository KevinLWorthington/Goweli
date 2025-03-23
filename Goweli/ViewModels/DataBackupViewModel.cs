using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Goweli.Data;
using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;

namespace Goweli.ViewModels
{
    public partial class DataBackupViewModel : ViewModelBase
    {
        private readonly GoweliDbContext _dbContext;
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private string _statusMessage = string.Empty;

        [ObservableProperty]
        private bool _isProcessing = false;

        [ObservableProperty]
        private string _exportedJson = string.Empty;

        [ObservableProperty]
        private string _importJson = string.Empty;

        [ObservableProperty]
        private bool _showExportData = false;

        [ObservableProperty]
        private bool _importSuccessful = false;

        [ObservableProperty]
        private int _importedCount = 0;

        [ObservableProperty]
        private ObservableCollection<Book> _allBooks = new();

        [ObservableProperty]
        private bool _showAdvancedInfo = false;

        [ObservableProperty]
        private string _advancedInfoButtonText = "SHOW DETAILS";

        public DataBackupViewModel(GoweliDbContext dbContext, MainViewModel mainViewModel)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));

            // Load books when the view model is created
            LoadBooks();
        }

        private async void LoadBooks()
        {
            await UpdateBooksList();
        }

        private async Task UpdateBooksList()
        {
            try
            {
                var books = await _dbContext.Books.ToListAsync();
                AllBooks = new ObservableCollection<Book>(books);
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading books: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task PrepareExport()
        {
            try
            {
                IsProcessing = true;
                StatusMessage = "Preparing data for export...";

                // Get all books from the database
                var books = await _dbContext.Books.ToListAsync();

                // Serialize to JSON with indentation for readability
                var options = new JsonSerializerOptions { WriteIndented = true };
                ExportedJson = JsonSerializer.Serialize(books, options);

                // Show the export data textarea
                ShowExportData = true;
                StatusMessage = "Data prepared! Copy the JSON text and save it to a file.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error preparing data: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void HideExport()
        {
            ShowExportData = false;
            StatusMessage = string.Empty;
        }

        [RelayCommand]
        private async Task ImportData()
        {
            if (string.IsNullOrWhiteSpace(ImportJson))
            {
                StatusMessage = "Please enter JSON data to import";
                await Task.Delay(3000);
                StatusMessage = string.Empty;
                return;
            }

            try
            {
                IsProcessing = true;
                StatusMessage = "Processing import data...";

                // Try to deserialize the JSON
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var books = JsonSerializer.Deserialize<List<Book>>(ImportJson, options);

                if (books == null || books.Count == 0)
                {
                    StatusMessage = "No valid book data found in the import text";
                    IsProcessing = false;
                    return;
                }

                // Process each book
                int importCount = 0;
                foreach (var book in books)
                {
                    // Reset the ID to let the database assign a new one
                    book.Id = 0;

                    // Check if the book already exists (by title and author)
                    var existingBook = await _dbContext.Books
                        .FirstOrDefaultAsync(b =>
                            b.BookTitle == book.BookTitle &&
                            b.AuthorName == book.AuthorName);

                    if (existingBook == null)
                    {
                        // Add new book
                        _dbContext.Books.Add(book);
                        importCount++;
                    }
                }

                await _dbContext.SaveChangesAsync();

                ImportSuccessful = true;
                ImportedCount = importCount;
                StatusMessage = $"Successfully imported {importCount} new books.";
                ImportJson = string.Empty;

                // Update the books list
                await UpdateBooksList();
            }
            catch (JsonException)
            {
                StatusMessage = "Invalid JSON format. Please check your data and try again.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error importing books: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
            }
        }

        [RelayCommand]
        private void ToggleAdvancedInfo()
        {
            ShowAdvancedInfo = !ShowAdvancedInfo;
            AdvancedInfoButtonText = ShowAdvancedInfo ? "HIDE DETAILS" : "SHOW DETAILS";
        }
    }
}