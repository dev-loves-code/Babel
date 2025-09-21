using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Wishlist;
using api.Interfaces;
using api.Models;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IWishlistRepository _wishlistRepository;
        private readonly IBookRepository _bookRepository;
        private readonly IBorrowService _borrowService;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<WishlistService> _logger;

        private const string USER_WISHLIST_CACHE_KEY_PREFIX = "UserWishlist_";

        public WishlistService(
            IWishlistRepository wishlistRepository,
            IBookRepository bookRepository,
            IBorrowService borrowService,
            IRedisCacheService redisCacheService,
            ILogger<WishlistService> logger)
        {
            _wishlistRepository = wishlistRepository;
            _bookRepository = bookRepository;
            _borrowService = borrowService;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetWishlistDto>> GetUserWishlistAsync(string userId, WishlistQueryObject queryObject)
        {
            _logger.LogInformation("Fetching wishlist for user {UserId}", userId);

            try
            {
                string cacheKey = BuildCacheKey(userId, queryObject);
                var cachedWishlist = _redisCacheService.GetData<IEnumerable<GetWishlistDto>>(cacheKey);

                if (cachedWishlist != null)
                {
                    _logger.LogInformation("Retrieved {ItemCount} wishlist items from cache", cachedWishlist.Count());
                    return cachedWishlist;
                }

                _logger.LogInformation("Cache miss - fetching wishlist from repository");
                var wishlistItems = await _wishlistRepository.GetUserWishlistAsync(userId);

                var wishlistDtos = new List<GetWishlistDto>();
                foreach (var item in wishlistItems)
                {
                    var dto = await MapToWishlistDto(item); // sequential await
                    wishlistDtos.Add(dto);
                }

                // Apply filters
                var filteredWishlist = ApplyFilters(wishlistDtos, queryObject);

                if (filteredWishlist.Any())
                {
                    _redisCacheService.SetData(cacheKey, filteredWishlist);
                    _logger.LogInformation("Cached {ItemCount} wishlist items", filteredWishlist.Count());
                }

                return filteredWishlist;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve wishlist for user {UserId}", userId);
                throw;
            }
        }

        public async Task<GetWishlistDto> AddToWishlistAsync(string userId, AddToWishlistDto wishlistDto)
        {
            _logger.LogInformation("Adding book {BookId} to wishlist for user {UserId}", wishlistDto.BookId, userId);

            try
            {
                // Validate book exists
                var book = await _bookRepository.GetBookByIdAsync(wishlistDto.BookId);
                if (book == null)
                {
                    _logger.LogWarning("Book {BookId} not found", wishlistDto.BookId);
                    throw new KeyNotFoundException($"Book with ID {wishlistDto.BookId} not found");
                }

                // Check if book is already in wishlist
                if (await _wishlistRepository.IsBookInWishlistAsync(userId, wishlistDto.BookId))
                {
                    _logger.LogWarning("Book {BookId} is already in user {UserId} wishlist", wishlistDto.BookId, userId);
                    throw new InvalidOperationException("This book is already in your wishlist");
                }

                var wishlistItem = new Wishlist
                {
                    UserId = userId,
                    BookId = wishlistDto.BookId
                };

                var createdItem = await _wishlistRepository.AddToWishlistAsync(wishlistItem);
                var completeItem = await _wishlistRepository.GetWishlistItemAsync(userId, wishlistDto.BookId);

                await InvalidateWishlistCacheAsync(userId);
                _logger.LogInformation("Successfully added book {BookId} to wishlist for user {UserId}", wishlistDto.BookId, userId);

                return await MapToWishlistDto(completeItem!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add book {BookId} to wishlist for user {UserId}", wishlistDto.BookId, userId);
                throw;
            }
        }

        public async Task<bool> RemoveFromWishlistAsync(string userId, int bookId)
        {
            _logger.LogInformation("Removing book {BookId} from wishlist for user {UserId}", bookId, userId);

            try
            {
                var result = await _wishlistRepository.RemoveFromWishlistAsync(userId, bookId);

                if (result)
                {
                    await InvalidateWishlistCacheAsync(userId);
                    _logger.LogInformation("Successfully removed book {BookId} from wishlist for user {UserId}", bookId, userId);
                }
                else
                {
                    _logger.LogWarning("Book {BookId} not found in user {UserId} wishlist", bookId, userId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove book {BookId} from wishlist for user {UserId}", bookId, userId);
                throw;
            }
        }

        public async Task<bool> IsBookInWishlistAsync(string userId, int bookId)
        {
            try
            {
                return await _wishlistRepository.IsBookInWishlistAsync(userId, bookId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if book {BookId} is in wishlist for user {UserId}", bookId, userId);
                throw;
            }
        }

        public async Task<int> GetWishlistCountAsync(string userId)
        {
            try
            {
                return await _wishlistRepository.GetWishlistCountAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get wishlist count for user {UserId}", userId);
                throw;
            }
        }

        private async Task<GetWishlistDto> MapToWishlistDto(Wishlist wishlist)
        {
            var isAvailable = await _borrowService.IsBookAvailableAsync(wishlist.BookId);

            return new GetWishlistDto
            {
                Id = wishlist.Id,
                BookId = wishlist.BookId,
                BookTitle = wishlist.Book?.Title ?? string.Empty,
                AuthorName = wishlist.Book?.Author?.Name ?? string.Empty,
                GenreName = wishlist.Book?.Genre?.Name ?? string.Empty,
                AddedDate = DateTime.UtcNow, // Since Wishlist model doesn't have CreatedDate, using current time
                IsAvailable = isAvailable
            };
        }

        private IEnumerable<GetWishlistDto> ApplyFilters(IEnumerable<GetWishlistDto> wishlist, WishlistQueryObject queryObject)
        {
            var query = wishlist.AsQueryable();

            if (!string.IsNullOrEmpty(queryObject.BookTitle))
                query = query.Where(w => w.BookTitle.Contains(queryObject.BookTitle, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(queryObject.AuthorName))
                query = query.Where(w => w.AuthorName.Contains(queryObject.AuthorName, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrEmpty(queryObject.GenreName))
                query = query.Where(w => w.GenreName.Contains(queryObject.GenreName, StringComparison.OrdinalIgnoreCase));

            if (queryObject.AvailableOnly == true)
                query = query.Where(w => w.IsAvailable);

            return query.ToList();
        }

        private string BuildCacheKey(string userId, WishlistQueryObject queryObject)
        {
            var parts = new List<string> { $"{USER_WISHLIST_CACHE_KEY_PREFIX}{userId}" };

            if (!string.IsNullOrEmpty(queryObject.BookTitle))
                parts.Add($"title:{queryObject.BookTitle.ToLower()}");

            if (!string.IsNullOrEmpty(queryObject.AuthorName))
                parts.Add($"author:{queryObject.AuthorName.ToLower()}");

            if (!string.IsNullOrEmpty(queryObject.GenreName))
                parts.Add($"genre:{queryObject.GenreName.ToLower()}");

            if (queryObject.AvailableOnly.HasValue)
                parts.Add($"available:{queryObject.AvailableOnly.Value}");

            return string.Join(":", parts);
        }

        private async Task InvalidateWishlistCacheAsync(string userId)
        {
            try
            {
                // Remove all cached variations for this user
                var pattern = $"{USER_WISHLIST_CACHE_KEY_PREFIX}{userId}*";
                _redisCacheService.RemoveData($"{USER_WISHLIST_CACHE_KEY_PREFIX}{userId}");
                _logger.LogDebug("Invalidated wishlist cache for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate wishlist cache for user {UserId}", userId);
            }
        }
    }
}