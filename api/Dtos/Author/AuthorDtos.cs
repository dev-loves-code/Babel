
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Author
{
    public class GetAuthorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<BookSummaryDto>? Books { get; set; }
    }

    public class CreateAuthorDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    public class UpdateAuthorDto
    {
        [Required, MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    public class BookSummaryDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string GenreName { get; set; } = string.Empty;
    }
}
