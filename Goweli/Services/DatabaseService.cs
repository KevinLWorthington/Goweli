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
        private readonly GoweliDbContext _dbContext;

        public DatabaseService(GoweliDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task InitializeAsync()
        {
            await _dbContext.Database.EnsureCreatedAsync();
        }

        public async Task<List<Book>> GetBooksAsync()
        {
            return await _dbContext.Books.ToListAsync();
        }

        public async Task AddBookAsync(Book book)
        {
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteBookAsync(Book book)
        {
            _dbContext.Books.Remove(book);
            await _dbContext.SaveChangesAsync();
        }
    }
}
