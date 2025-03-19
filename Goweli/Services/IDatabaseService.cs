using System.Collections.Generic;
using System.Threading.Tasks;
using Goweli.Models;

namespace Goweli.Services
{
    // IDatabaseService interface
    public interface IDatabaseService
    {
        Task InitializeAsync();
        Task<List<Book>> GetBooksAsync();
        Task AddBookAsync(Book book);
        Task DeleteBookAsync(Book book);
    }
}
