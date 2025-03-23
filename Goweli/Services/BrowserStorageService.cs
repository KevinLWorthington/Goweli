using System;
using System.Text.Json;
using System.Threading.Tasks;
using Goweli.Data;
using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.JSInterop;
using System.Collections.Generic;

namespace Goweli.Services
{
    public class BrowserStorageService
    {
        private readonly GoweliDbContext _dbContext;
        private readonly IJSRuntime _jsRuntime;

        public BrowserStorageService(GoweliDbContext dbContext, IJSRuntime jsRuntime)
        {
            _dbContext = dbContext;
            _jsRuntime = jsRuntime;
        }

        // Export all books as JSON and download
        public async Task ExportBooksAsync()
        {
            try
            {
                var books = await _dbContext.Books.ToListAsync();
                var json = JsonSerializer.Serialize(books, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                var fileName = $"goweli_books_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                await _jsRuntime.InvokeVoidAsync("downloadJson", fileName, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Export error: {ex.Message}");
                throw;
            }
        }

        // Import books from JSON
        public async Task<(bool Success, string Message, int Count)> ImportBooksAsync(string jsonContent)
        {
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var books = JsonSerializer.Deserialize<List<Book>>(jsonContent, options);

                if (books == null || books.Count == 0)
                {
                    return (false, "No books found in the file", 0);
                }

                int importCount = 0;
                foreach (var book in books)
                {
                    // Reset ID to let database assign new one
                    book.Id = 0;

                    // Check if book already exists
                    bool exists = await _dbContext.Books.AnyAsync(b =>
                        b.BookTitle == book.BookTitle &&
                        b.AuthorName == book.AuthorName);

                    if (!exists)
                    {
                        _dbContext.Books.Add(book);
                        importCount++;
                    }
                }

                await _dbContext.SaveChangesAsync();
                return (true, $"Successfully imported {importCount} books", importCount);
            }
            catch (JsonException)
            {
                return (false, "Invalid JSON format", 0);
            }
            catch (Exception ex)
            {
                return (false, $"Import error: {ex.Message}", 0);
            }
        }

        // Upload a JSON file
        public async Task<string> UploadJsonFileAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("uploadJsonFile");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}