using System.Collections.Generic;
using System.Threading.Tasks;
using api.Models;

namespace api.Interfaces
{
    public interface IWishlistRepository
    {
        Task<IEnumerable<Wishlist>> GetUserWishlistAsync(string userId);
        Task<Wishlist?> GetWishlistItemAsync(string userId, int bookId);
        Task<Wishlist> AddToWishlistAsync(Wishlist wishlistItem);
        Task<bool> RemoveFromWishlistAsync(int id);
        Task<bool> RemoveFromWishlistAsync(string userId, int bookId);
        Task<bool> IsBookInWishlistAsync(string userId, int bookId);
        Task<int> GetWishlistCountAsync(string userId);
    }
}