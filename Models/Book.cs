using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Book class to set up the properties of a book as entered by the user
namespace Goweli.Models
{
    public class Book
    {
        public int Id { get; set; }

        public string BookTitle { get; set; } = string.Empty;

        public string AuthorName { get; set; } = string.Empty;

        public string? ISBN { get; set; }

        public string Synopsis { get; set; } = string.Empty;

        public bool IsChecked { get; set; }
    }
}
