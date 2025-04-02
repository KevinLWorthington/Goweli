using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Goweli.Models;

namespace Goweli.Services
{
    public class DatabaseService : IDatabaseService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiBaseUrl = "http://localhost:5128/api/Books";

        public DatabaseService(HttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public async Task InitializeAsync()
        {
            await Task.CompletedTask;
        }

        public async Task<List<Book>> GetBooksAsync()
        {
            try
            {
                var books = await _httpClient.GetFromJsonAsync<List<Book>>(_apiBaseUrl);
                return books ?? new List<Book>();
            }
            catch (Exception)
            {
                // Return empty list on error
                return new List<Book>();
            }
        }

        public async Task AddBookAsync(Book book)
        {
            await _httpClient.PostAsJsonAsync(_apiBaseUrl, book);
        }

        public async Task DeleteBookAsync(Book book)
        {
            await _httpClient.DeleteAsync($"{_apiBaseUrl}/{book.Id}");
        }
    }
}
