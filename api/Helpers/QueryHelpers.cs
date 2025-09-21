using System;

namespace api.Helpers
{
    public class BookQueryObject
    {
        public string? Title { get; set; }
        public string? GenreName { get; set; }
        public string? AuthorName { get; set; }
        public int? MinQuantity { get; set; }
        public int? MaxQuantity { get; set; }
    }

    public class AuthorQueryObject
    {
        public string? Name { get; set; }
    }

    public class GenreQueryObject
    {
        public string? Name { get; set; }
    }
}