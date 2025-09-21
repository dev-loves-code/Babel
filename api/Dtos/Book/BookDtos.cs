using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Book
{
    public class GetBookDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int GenreId { get; set; }
        public string GenreName { get; set; } = string.Empty;
        public int AuthorId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }

    public class CreateBookDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int Quantity { get; set; }

        [Required]
        public int GenreId { get; set; }

        [Required]
        public int AuthorId { get; set; }
    }

    public class UpdateBookDto
    {
        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
        public int Quantity { get; set; }

        [Required]
        public int GenreId { get; set; }

        [Required]
        public int AuthorId { get; set; }
    }
}
