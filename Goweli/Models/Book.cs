using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Goweli.Models
{
    // Book model
    public class Book
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string BookTitle { get; set; } = string.Empty;

        [Required]
        public string AuthorName { get; set; } = string.Empty;

        public string? ISBN { get; set; }

        public string? Synopsis { get; set; }

        public bool IsChecked { get; set; }

        public string? CoverUrl { get; set; }
    }
}