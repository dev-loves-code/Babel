
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Genre
{
    public class GetGenreDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<BookSummaryDto>? Books { get; set; }
    }

    public class CreateGenreDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateGenreDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    public class BookSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string AuthorName { get; set; } = string.Empty;
    }
}
