// Book class to set up the properties of a book as entered by the user
namespace Goweli.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string? ISBN { get; set; }
        public string? Synopsis { get; set; }
        public bool IsChecked { get; set; }
        public string? CoverUrl { get; set; } = string.Empty;
    }
}
