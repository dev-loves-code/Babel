using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Wishlist;

namespace api.Interfaces
{
    public interface IWishlistService
    {
        Task<IEnumerable<GetWishlistDto>> GetUserWishlistAsync(string userId, WishlistQueryObject queryObject);
        Task<GetWishlistDto> AddToWishlistAsync(string userId, AddToWishlistDto wishlistDto);
        Task<bool> RemoveFromWishlistAsync(string userId, int bookId);
        Task<bool> IsBookInWishlistAsync(string userId, int bookId);
        Task<int> GetWishlistCountAsync(string userId);
    }
}