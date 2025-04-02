using Goweli.Data;
using Goweli.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Goweli.Services
{
    // DatabaseService class implements IDatabaseService interface
    public class DatabaseService : IDatabaseService
    {
        // Dependency injection
        private readonly GoweliDbContext _dbContext;

        // Constructor with dependency injection
        public DatabaseService(GoweliDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // Initializes the database and ensures it is created
        public async Task InitializeAsync()
        {
            await _dbContext.Database.EnsureCreatedAsync();
        }

        // Retrieves all books from the database
        public async Task<List<Book>> GetBooksAsync()
        {
            return await _dbContext.Books.ToListAsync();
        }

        // Adds a book to the database
        public async Task AddBookAsync(Book book)
        {
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();
        }

        // Deletes a book from the database
        public async Task DeleteBookAsync(Book book)
        {
            _dbContext.Books.Remove(book);
            await _dbContext.SaveChangesAsync();
        }
    }
}
