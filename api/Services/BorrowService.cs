using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Borrow;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class BorrowService : IBorrowService
    {
        private readonly IBorrowRepository _borrowRepository;
        private readonly IBookRepository _bookRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<BorrowService> _logger;

        private const string USER_BORROWS_CACHE_KEY_PREFIX = "UserBorrows_";
        private const string BORROW_CACHE_KEY_PREFIX = "Borrow_";
        private const string ALL_BORROWS_CACHE_KEY_PREFIX = "AllBorrows_";
        private const int MAX_ACTIVE_BORROWS = 3;
        private const int BORROW_DURATION_DAYS = 7;

        public BorrowService(
            IBorrowRepository borrowRepository,
            IBookRepository bookRepository,
            UserManager<AppUser> userManager,
            IRedisCacheService redisCacheService,
            ILogger<BorrowService> logger)
        {
            _borrowRepository = borrowRepository;
            _bookRepository = bookRepository;
            _userManager = userManager;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetBorrowDto>> GetAllBorrowsAsync(BorrowQueryObject queryObject)
        {
            _logger.LogInformation("Starting to fetch all borrows");

            try
            {

                var borrows = await _borrowRepository.GetAllBorrowsAsync(queryObject);
                var borrowDtos = borrows.Select(MapToBorrowDto);

                if (queryObject.Status.HasValue)
                {
                    borrowDtos = borrowDtos.Where(b => b.Status == queryObject.Status.Value);
                }

                return borrowDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all borrows");
                throw;
            }
        }


        public async Task<GetBorrowDto?> GetBorrowByBookAndUserAsync(int bookId, string userId)
        {
            _logger.LogInformation("Fetching borrow for Book ID {BookId} and User {UserId}", bookId, userId);

            try
            {
                string cacheKey = $"{BORROW_CACHE_KEY_PREFIX}_BOOK_{bookId}_USER_{userId}";
                var cachedBorrow = _redisCacheService.GetData<GetBorrowDto>(cacheKey);

                if (cachedBorrow != null)
                {
                    _logger.LogInformation("Retrieved borrow for Book ID {BookId} and User {UserId} from cache", bookId, userId);
                    return cachedBorrow;
                }

                _logger.LogInformation("Cache miss - fetching borrow from repository");
                var borrow = await _borrowRepository.GetBorrowByBookAndUserAsync(bookId, userId);

                if (borrow == null)
                {
                    _logger.LogWarning("Borrow for Book ID {BookId} and User {UserId} not found", bookId, userId);
                    return null;
                }

                var borrowDto = MapToBorrowDto(borrow);
                _redisCacheService.SetData(cacheKey, borrowDto);
                _logger.LogInformation("Cached borrow for Book ID {BookId} and User {UserId}", bookId, userId);

                return borrowDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve borrow for Book ID {BookId} and User {UserId}", bookId, userId);
                throw;
            }
        }



        public async Task<IEnumerable<GetBorrowDto>> GetUserBorrowsAsync(string userId)
        {
            _logger.LogInformation("Fetching borrows for user {UserId}", userId);

            try
            {
                string cacheKey = $"{USER_BORROWS_CACHE_KEY_PREFIX}{userId}";
                var cachedBorrows = _redisCacheService.GetData<IEnumerable<GetBorrowDto>>(cacheKey);

                if (cachedBorrows != null)
                {
                    _logger.LogInformation("Retrieved {BorrowCount} user borrows from cache", cachedBorrows.Count());
                    return cachedBorrows;
                }

                var borrows = await _borrowRepository.GetActiveBorrowsByUserAsync(userId);
                var borrowDtos = borrows.Select(MapToBorrowDto);

                _redisCacheService.SetData(cacheKey, borrowDtos);
                _logger.LogInformation("Cached {BorrowCount} user borrows", borrowDtos.Count());

                return borrowDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve borrows for user {UserId}", userId);
                throw;
            }
        }

        public async Task<GetBorrowDto> CreateBorrowAsync(string userId, CreateBorrowDto borrowDto)
        {
            _logger.LogInformation("Creating new borrow for user {UserId} and book {BookId}", userId, borrowDto.BookId);

            try
            {
                // Validate user is not blocked
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    throw new KeyNotFoundException($"User with ID {userId} not found");

                if (user.IsBlocked)
                {
                    _logger.LogWarning("User {UserId} is blocked and cannot borrow books", userId);
                    throw new InvalidOperationException("Your account is blocked and cannot borrow books");
                }

                // Validate user doesn't exceed borrow limit
                if (!await CanUserBorrowAsync(userId))
                {
                    _logger.LogWarning("User {UserId} has reached maximum active borrows", userId);
                    throw new InvalidOperationException($"You have reached the maximum of {MAX_ACTIVE_BORROWS} active borrows");
                }

                // Validate book exists and is available
                var book = await _bookRepository.GetBookByIdAsync(borrowDto.BookId);
                if (book == null)
                    throw new KeyNotFoundException($"Book with ID {borrowDto.BookId} not found");

                if (!await IsBookAvailableAsync(borrowDto.BookId))
                {
                    _logger.LogWarning("Book {BookId} is not available for borrowing", borrowDto.BookId);
                    throw new InvalidOperationException("This book is currently not available for borrowing");
                }

                // Check if user already has an active borrow for this book
                if (await _borrowRepository.HasActiveBorrowForBookAsync(userId, borrowDto.BookId))
                {
                    _logger.LogWarning("User {UserId} already has an active borrow for book {BookId}", userId, borrowDto.BookId);
                    throw new InvalidOperationException("You already have an active borrow for this book");
                }

                var borrow = new Borrow
                {
                    BookId = borrowDto.BookId,
                    UserId = userId,
                    StartDate = DateTime.UtcNow
                };

                var createdBorrow = await _borrowRepository.CreateBorrowAsync(borrow);
                var completeBorrow = await _borrowRepository.GetBorrowByIdAsync(createdBorrow.Id);

                await InvalidateBorrowCacheAsync(userId, borrowDto.BookId);
                _logger.LogInformation("Successfully created borrow {BorrowId} for user {UserId}", createdBorrow.Id, userId);

                return MapToBorrowDto(completeBorrow!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create borrow");
                throw;
            }
        }

        public async Task<GetBorrowDto> CompleteBorrowAsync(int borrowId)
        {
            _logger.LogInformation("Completing borrow {BorrowId}", borrowId);

            try
            {
                var borrow = await _borrowRepository.GetBorrowByIdAsync(borrowId);
                if (borrow == null)
                    throw new KeyNotFoundException($"Borrow with ID {borrowId} not found");

                if (borrow.ReturnDate.HasValue)
                {
                    _logger.LogWarning("Borrow {BorrowId} is already returned", borrowId);
                    throw new InvalidOperationException("This borrow has already been returned");
                }

                borrow.ReturnDate = DateTime.UtcNow;
                var updatedBorrow = await _borrowRepository.UpdateBorrowAsync(borrow);

                await InvalidateBorrowCacheAsync(borrow.UserId, borrow.BookId);
                _logger.LogInformation("Successfully completed borrow {BorrowId}", borrowId);

                return MapToBorrowDto(updatedBorrow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to complete borrow {BorrowId}", borrowId);
                throw;
            }
        }

        public async Task<bool> CanUserBorrowAsync(string userId)
        {
            try
            {
                var activeBorrowCount = await _borrowRepository.GetActiveBorrowCountByUserAsync(userId);
                return activeBorrowCount < MAX_ACTIVE_BORROWS;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if user {UserId} can borrow", userId);
                throw;
            }
        }

        public async Task<bool> IsBookAvailableAsync(int bookId)
        {
            try
            {
                var book = await _bookRepository.GetBookByIdAsync(bookId);
                if (book == null) return false;

                var activeBorrowCount = await _borrowRepository.GetActiveAndPastDueBorrowCountByBookAsync(bookId);
                return activeBorrowCount < book.Quantity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check availability for book {BookId}", bookId);
                throw;
            }
        }

        private GetBorrowDto MapToBorrowDto(Borrow borrow)
        {
            var dueDate = borrow.StartDate.AddDays(BORROW_DURATION_DAYS);
            var status = ComputeBorrowStatus(borrow, dueDate);

            return new GetBorrowDto
            {
                Id = borrow.Id,
                BookId = borrow.BookId,
                BookTitle = borrow.Book?.Title ?? string.Empty,
                UserId = borrow.UserId,
                UserName = borrow.User?.UserName ?? string.Empty,
                StartDate = borrow.StartDate,
                DueDate = dueDate,
                ReturnDate = borrow.ReturnDate,
                Status = status,
                HasPendingReturnRequest = borrow.ReturnRequest?.Status == ReturnStatus.Pending
            };
        }

        private BorrowStatus ComputeBorrowStatus(Borrow borrow, DateTime dueDate)
        {
            if (borrow.ReturnDate.HasValue)
            {
                return borrow.ReturnDate.Value > dueDate ? BorrowStatus.ReturnedPastDue : BorrowStatus.Returned;
            }

            return DateTime.UtcNow > dueDate ? BorrowStatus.PastDue : BorrowStatus.Active;
        }

        private string BuildCacheKey(BorrowQueryObject queryObject)
        {
            var parts = new List<string> { ALL_BORROWS_CACHE_KEY_PREFIX };

            if (!string.IsNullOrEmpty(queryObject.UserId))
                parts.Add($"user:{queryObject.UserId}");

            if (!string.IsNullOrEmpty(queryObject.UserName))
                parts.Add($"username:{queryObject.UserName.ToLower()}");

            if (!string.IsNullOrEmpty(queryObject.BookTitle))
                parts.Add($"book:{queryObject.BookTitle.ToLower()}");

            if (queryObject.Status.HasValue)
                parts.Add($"status:{queryObject.Status.Value}");

            if (queryObject.IncludeReturned.HasValue)
                parts.Add($"includeReturned:{queryObject.IncludeReturned.Value}");

            return string.Join(":", parts);
        }

        private async Task InvalidateBorrowCacheAsync(string userId, int bookId)
        {
            try
            {
                _redisCacheService.RemoveData($"{USER_BORROWS_CACHE_KEY_PREFIX}{userId}");
                _redisCacheService.RemoveData(ALL_BORROWS_CACHE_KEY_PREFIX);
                _logger.LogDebug("Invalidated borrow cache for user {UserId} and book {BookId}", userId, bookId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate borrow cache");
            }
        }

        // Add these methods to your BorrowService class

        public async Task<IEnumerable<GetBorrowDto>> GetPastDueBorrowsAsync(string userId)
        {
            _logger.LogInformation("Fetching past due borrows for user {UserId}", userId);

            try
            {
                string cacheKey = $"PastDueBorrows_{userId}";
                var cachedBorrows = _redisCacheService.GetData<IEnumerable<GetBorrowDto>>(cacheKey);

                if (cachedBorrows != null)
                {
                    _logger.LogInformation("Retrieved {BorrowCount} past due borrows from cache", cachedBorrows.Count());
                    return cachedBorrows;
                }

                var queryObject = new BorrowQueryObject
                {
                    UserId = userId,
                    Status = BorrowStatus.PastDue,
                    IncludeReturned = false
                };

                var borrows = await _borrowRepository.GetAllBorrowsAsync(queryObject);
                var borrowDtos = borrows.Select(MapToBorrowDto);

                _redisCacheService.SetData(cacheKey, borrowDtos, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Cached {BorrowCount} past due borrows", borrowDtos.Count());

                return borrowDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve past due borrows for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<GetBorrowDto>> GetActiveBorrowsAsync(string userId)
        {
            _logger.LogInformation("Fetching active borrows for user {UserId}", userId);

            try
            {
                string cacheKey = $"ActiveBorrows_{userId}";
                var cachedBorrows = _redisCacheService.GetData<IEnumerable<GetBorrowDto>>(cacheKey);

                if (cachedBorrows != null)
                {
                    _logger.LogInformation("Retrieved {BorrowCount} active borrows from cache", cachedBorrows.Count());
                    return cachedBorrows;
                }

                var queryObject = new BorrowQueryObject
                {
                    UserId = userId,
                    Status = BorrowStatus.Active,
                    IncludeReturned = false
                };

                var borrows = await _borrowRepository.GetAllBorrowsAsync(queryObject);
                var borrowDtos = borrows.Select(MapToBorrowDto);

                _redisCacheService.SetData(cacheKey, borrowDtos, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Cached {BorrowCount} active borrows", borrowDtos.Count());

                return borrowDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve active borrows for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<GetBorrowDto>> GetRecentlyReturnedBorrowsAsync(string userId, int daysBack = 7)
        {
            _logger.LogInformation("Fetching recently returned borrows for user {UserId} from last {Days} days", userId, daysBack);

            try
            {
                string cacheKey = $"RecentlyReturned_{userId}_{daysBack}";
                var cachedBorrows = _redisCacheService.GetData<IEnumerable<GetBorrowDto>>(cacheKey);

                if (cachedBorrows != null)
                {
                    _logger.LogInformation("Retrieved {BorrowCount} recently returned borrows from cache", cachedBorrows.Count());
                    return cachedBorrows;
                }

                var fromDate = DateTime.UtcNow.AddDays(-daysBack);
                var queryObject = new BorrowQueryObject
                {
                    UserId = userId,
                    IncludeReturned = true,
                    StartDateFrom = fromDate
                };

                var borrows = await _borrowRepository.GetAllBorrowsAsync(queryObject);
                var returnedBorrows = borrows
                    .Where(b => b.ReturnDate.HasValue && b.ReturnDate.Value >= fromDate)
                    .Select(MapToBorrowDto);

                _redisCacheService.SetData(cacheKey, returnedBorrows, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Cached {BorrowCount} recently returned borrows", returnedBorrows.Count());

                return returnedBorrows;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve recently returned borrows for user {UserId}", userId);
                throw;
            }
        }
    }

}