using GoweliProxyApi.Data;
using GoweliProxyApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GoweliProxyApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BooksController : ControllerBase
    {
        private readonly GoweliDbContext _dbContext;
        private readonly ILogger<BooksController> _logger;

        public BooksController(GoweliDbContext dbContext, ILogger<BooksController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        // GET: api/books
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Book>>> GetBooks()
        {
            _logger.LogInformation("Getting all books");
            return await _dbContext.Books.ToListAsync();
        }

        // GET: api/books/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            _logger.LogInformation($"Getting book with ID: {id}");
            var book = await _dbContext.Books.FindAsync(id);

            if (book == null)
            {
                _logger.LogWarning($"Book with ID {id} not found");
                return NotFound();
            }

            return book;
        }

        // POST: api/books
        [HttpPost]
        public async Task<ActionResult<Book>> PostBook(Book book)
        {
            _logger.LogInformation($"Adding new book: {book.BookTitle}");
            _dbContext.Books.Add(book);
            await _dbContext.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        // PUT: api/books/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutBook(int id, Book book)
        {
            if (id != book.Id)
            {
                return BadRequest();
            }

            _logger.LogInformation($"Updating book with ID: {id}");
            _dbContext.Entry(book).State = EntityState.Modified;

            try
            {
                await _dbContext.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _dbContext.Books.AnyAsync(e => e.Id == id))
                {
                    _logger.LogWarning($"Book with ID {id} not found during update");
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // DELETE: api/books/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            _logger.LogInformation($"Deleting book with ID: {id}");
            var book = await _dbContext.Books.FindAsync(id);
            if (book == null)
            {
                _logger.LogWarning($"Book with ID {id} not found during delete attempt");
                return NotFound();
            }

            _dbContext.Books.Remove(book);
            await _dbContext.SaveChangesAsync();

            return NoContent();
        }
    }
}
