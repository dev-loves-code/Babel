using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace api.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<WishlistRepository> _logger;

        public WishlistRepository(ApplicationDBContext context, ILogger<WishlistRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Wishlist>> GetUserWishlistAsync(string userId)
        {
            return await _context.Wishlists
                .Include(w => w.Book)
                    .ThenInclude(b => b!.Author)
                .Include(w => w.Book)
                    .ThenInclude(b => b!.Genre)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.Id) // Most recently added first
                .ToListAsync();
        }

        public async Task<Wishlist?> GetWishlistItemAsync(string userId, int bookId)
        {
            return await _context.Wishlists
                .Include(w => w.Book)
                    .ThenInclude(b => b!.Author)
                .Include(w => w.Book)
                    .ThenInclude(b => b!.Genre)
                .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId);
        }

        public async Task<Wishlist> AddToWishlistAsync(Wishlist wishlistItem)
        {
            _context.Wishlists.Add(wishlistItem);
            await _context.SaveChangesAsync();
            return wishlistItem;
        }

        public async Task<bool> RemoveFromWishlistAsync(int id)
        {
            var wishlistItem = await _context.Wishlists.FindAsync(id);
            if (wishlistItem == null)
                return false;

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromWishlistAsync(string userId, int bookId)
        {
            var wishlistItem = await _context.Wishlists
                .FirstOrDefaultAsync(w => w.UserId == userId && w.BookId == bookId);

            if (wishlistItem == null)
                return false;

            _context.Wishlists.Remove(wishlistItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsBookInWishlistAsync(string userId, int bookId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.BookId == bookId);
        }

        public async Task<int> GetWishlistCountAsync(string userId)
        {
            return await _context.Wishlists
                .CountAsync(w => w.UserId == userId);
        }
    }
}