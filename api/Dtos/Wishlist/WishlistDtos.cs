using System;
using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Wishlist
{
    public class GetWishlistDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string GenreName { get; set; } = string.Empty;
        public DateTime AddedDate { get; set; }
        public bool IsAvailable { get; set; }
    }

    public class AddToWishlistDto
    {
        [Required]
        public int BookId { get; set; }
    }

    public class WishlistQueryObject
    {
        public string? BookTitle { get; set; }
        public string? AuthorName { get; set; }
        public string? GenreName { get; set; }
        public bool? AvailableOnly { get; set; }
    }
}