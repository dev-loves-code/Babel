using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.ReturnRequest;
using api.Dtos.Borrow;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Hangfire;

namespace api.Services
{
    public class ReturnRequestService : IReturnRequestService
    {
        private readonly IReturnRequestRepository _returnRequestRepository;
        private readonly IBorrowRepository _borrowRepository;
        private readonly IBorrowService _borrowService;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<ReturnRequestService> _logger;

        private const string USER_RETURN_REQUESTS_CACHE_KEY_PREFIX = "UserReturnRequests_";
        private const string RETURN_REQUEST_CACHE_KEY_PREFIX = "ReturnRequest_";
        private const string ALL_RETURN_REQUESTS_CACHE_KEY_PREFIX = "AllReturnRequests_";

        public ReturnRequestService(
            IReturnRequestRepository returnRequestRepository,
            IBorrowRepository borrowRepository,
            IBorrowService borrowService,
            IRedisCacheService redisCacheService,
            ILogger<ReturnRequestService> logger)
        {
            _returnRequestRepository = returnRequestRepository;
            _borrowRepository = borrowRepository;
            _borrowService = borrowService;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetReturnRequestDto>> GetAllReturnRequestsAsync(ReturnRequestQueryObject queryObject)
        {
            _logger.LogInformation("Starting to fetch all return requests");

            try
            {
                string cacheKey = BuildCacheKey(queryObject);
                var cachedRequests = _redisCacheService.GetData<IEnumerable<GetReturnRequestDto>>(cacheKey);

                if (cachedRequests != null)
                {
                    _logger.LogInformation("Retrieved {RequestCount} return requests from cache", cachedRequests.Count());
                    return cachedRequests;
                }

                _logger.LogInformation("Cache miss - fetching return requests from repository");
                var returnRequests = await _returnRequestRepository.GetAllReturnRequestsAsync(queryObject);
                var requestDtos = returnRequests.Select(MapToReturnRequestDto);

                if (requestDtos.Any())
                {
                    _redisCacheService.SetData(cacheKey, requestDtos);
                    _logger.LogInformation("Cached {RequestCount} return requests", requestDtos.Count());
                }

                return requestDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all return requests");
                throw;
            }
        }

        public async Task<GetReturnRequestDto?> GetReturnRequestByIdAsync(int id)
        {
            _logger.LogInformation("Fetching return request with ID {RequestId}", id);

            try
            {
                string cacheKey = $"{RETURN_REQUEST_CACHE_KEY_PREFIX}{id}";
                var cachedRequest = _redisCacheService.GetData<GetReturnRequestDto>(cacheKey);

                if (cachedRequest != null)
                {
                    _logger.LogInformation("Retrieved return request {RequestId} from cache", id);
                    return cachedRequest;
                }

                var returnRequest = await _returnRequestRepository.GetReturnRequestByIdAsync(id);
                if (returnRequest == null)
                {
                    _logger.LogWarning("Return request {RequestId} not found", id);
                    return null;
                }

                var requestDto = MapToReturnRequestDto(returnRequest);
                _redisCacheService.SetData(cacheKey, requestDto);

                return requestDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve return request {RequestId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<GetReturnRequestDto>> GetUserReturnRequestsAsync(string userId)
        {
            _logger.LogInformation("Fetching return requests for user {UserId}", userId);

            try
            {
                string cacheKey = $"{USER_RETURN_REQUESTS_CACHE_KEY_PREFIX}{userId}";
                var cachedRequests = _redisCacheService.GetData<IEnumerable<GetReturnRequestDto>>(cacheKey);

                if (cachedRequests != null)
                {
                    _logger.LogInformation("Retrieved {RequestCount} user return requests from cache", cachedRequests.Count());
                    return cachedRequests;
                }

                var queryObject = new ReturnRequestQueryObject { UserId = userId };
                var returnRequests = await _returnRequestRepository.GetAllReturnRequestsAsync(queryObject);
                var requestDtos = returnRequests.Select(MapToReturnRequestDto);

                _redisCacheService.SetData(cacheKey, requestDtos);
                return requestDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve return requests for user {UserId}", userId);
                throw;
            }
        }

        public async Task<GetReturnRequestDto> CreateReturnRequestAsync(string userId, CreateReturnRequestDto requestDto)
        {
            _logger.LogInformation("Creating return request for borrow {BorrowId} by user {UserId}", requestDto.BorrowId, userId);

            try
            {
                var borrow = await _borrowRepository.GetBorrowByIdAsync(requestDto.BorrowId);
                if (borrow == null)
                    throw new KeyNotFoundException($"Borrow with ID {requestDto.BorrowId} not found");

                if (borrow.UserId != userId)
                {
                    _logger.LogWarning("User {UserId} attempted to create return request for borrow {BorrowId} belonging to another user", userId, requestDto.BorrowId);
                    throw new UnauthorizedAccessException("You can only create return requests for your own borrows");
                }

                if (borrow.ReturnDate.HasValue)
                {
                    _logger.LogWarning("Attempted to create return request for already returned borrow {BorrowId}", requestDto.BorrowId);
                    throw new InvalidOperationException("This borrow has already been returned");
                }

                if (await _returnRequestRepository.HasPendingReturnRequestAsync(requestDto.BorrowId))
                {
                    _logger.LogWarning("Attempted to create duplicate return request for borrow {BorrowId}", requestDto.BorrowId);
                    throw new InvalidOperationException("A return request for this borrow is already pending");
                }

                var returnRequest = new ReturnRequest
                {
                    BorrowId = requestDto.BorrowId,
                    UserId = userId,
                    RequestDate = DateTime.UtcNow,
                    Status = ReturnStatus.Pending
                };

                var createdRequest = await _returnRequestRepository.CreateReturnRequestAsync(returnRequest);
                var completeRequest = await _returnRequestRepository.GetReturnRequestByIdAsync(createdRequest.Id);

                await InvalidateReturnRequestCacheAsync(userId);
                _logger.LogInformation("Successfully created return request {RequestId} for borrow {BorrowId}", createdRequest.Id, requestDto.BorrowId);

                return MapToReturnRequestDto(completeRequest!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create return request");
                throw;
            }
        }

        public async Task<GetReturnRequestDto> ProcessReturnRequestAsync(int requestId, ProcessReturnRequestDto processDto)
        {
            _logger.LogInformation("Processing return request {RequestId} with status {Status}", requestId, processDto.Status);

            try
            {
                var returnRequest = await _returnRequestRepository.GetReturnRequestByIdAsync(requestId);
                if (returnRequest == null)
                    throw new KeyNotFoundException($"Return request with ID {requestId} not found");

                if (returnRequest.Status != ReturnStatus.Pending)
                {
                    _logger.LogWarning("Attempted to process non-pending return request {RequestId}", requestId);
                    throw new InvalidOperationException("This return request has already been processed");
                }

                if (processDto.Status == ReturnStatus.Pending)
                    throw new ArgumentException("Cannot set return request status to Pending");

                returnRequest.Status = processDto.Status;
                var updatedRequest = await _returnRequestRepository.UpdateReturnRequestAsync(returnRequest);

                // If accepted, complete the borrow
                if (processDto.Status == ReturnStatus.Accepted)
                {
                    BackgroundJob.Schedule(() => _borrowService.CompleteBorrowAsync(returnRequest.BorrowId), TimeSpan.FromMinutes(5));
                    _logger.LogInformation("Completed borrow {BorrowId} after accepting return request {RequestId}", returnRequest.BorrowId, requestId);
                }

                await InvalidateReturnRequestCacheAsync(returnRequest.UserId);
                _logger.LogInformation("Successfully processed return request {RequestId} with status {Status}", requestId, processDto.Status);

                return MapToReturnRequestDto(updatedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process return request {RequestId}", requestId);
                throw;
            }
        }

        public async Task<GetBorrowDto> DirectReturnAsync(DirectReturnDto directReturnDto)
        {
            _logger.LogInformation("Processing direct return for borrow {BorrowId}", directReturnDto.BorrowId);

            try
            {
                var borrow = await _borrowRepository.GetBorrowByIdAsync(directReturnDto.BorrowId);
                if (borrow == null)
                    throw new KeyNotFoundException($"Borrow with ID {directReturnDto.BorrowId} not found");

                if (borrow.ReturnDate.HasValue)
                    throw new InvalidOperationException("This borrow has already been returned");

                // Check if there's a pending return request and mark it as accepted
                var pendingRequest = await _returnRequestRepository.GetReturnRequestByBorrowIdAsync(directReturnDto.BorrowId);
                if (pendingRequest?.Status == ReturnStatus.Pending)
                {
                    pendingRequest.Status = ReturnStatus.Accepted;
                    await _returnRequestRepository.UpdateReturnRequestAsync(pendingRequest);
                    _logger.LogInformation("Marked pending return request {RequestId} as accepted", pendingRequest.Id);
                }

                var completedBorrow = await _borrowService.CompleteBorrowAsync(directReturnDto.BorrowId);
                await InvalidateReturnRequestCacheAsync(borrow.UserId);

                _logger.LogInformation("Successfully processed direct return for borrow {BorrowId}", directReturnDto.BorrowId);
                return completedBorrow;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process direct return for borrow {BorrowId}", directReturnDto.BorrowId);
                throw;
            }
        }

        private GetReturnRequestDto MapToReturnRequestDto(ReturnRequest returnRequest)
        {
            var dueDate = returnRequest.Borrow!.StartDate.AddDays(7);
            var isOverdue = !returnRequest.Borrow.ReturnDate.HasValue && DateTime.UtcNow > dueDate;

            return new GetReturnRequestDto
            {
                Id = returnRequest.Id,
                BorrowId = returnRequest.BorrowId,
                BookTitle = returnRequest.Borrow.Book?.Title ?? string.Empty,
                UserId = returnRequest.UserId,
                UserName = returnRequest.User?.UserName ?? string.Empty,
                RequestDate = returnRequest.RequestDate,
                Status = returnRequest.Status,
                StartDate = returnRequest.Borrow.StartDate,
                DueDate = dueDate,
                IsOverdue = isOverdue
            };
        }

        private string BuildCacheKey(ReturnRequestQueryObject queryObject)
        {
            var parts = new List<string> { ALL_RETURN_REQUESTS_CACHE_KEY_PREFIX };

            if (!string.IsNullOrEmpty(queryObject.UserId))
                parts.Add($"user:{queryObject.UserId}");

            if (!string.IsNullOrEmpty(queryObject.UserName))
                parts.Add($"username:{queryObject.UserName.ToLower()}");

            if (queryObject.Status.HasValue)
                parts.Add($"status:{queryObject.Status.Value}");

            return string.Join(":", parts);
        }

        private async Task InvalidateReturnRequestCacheAsync(string userId)
        {
            try
            {
                _redisCacheService.RemoveData($"{USER_RETURN_REQUESTS_CACHE_KEY_PREFIX}{userId}");
                _redisCacheService.RemoveData(ALL_RETURN_REQUESTS_CACHE_KEY_PREFIX);
                _logger.LogDebug("Invalidated return request cache for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate return request cache");
            }
        }


        // Add these methods to your ReturnRequestService class

        public async Task<IEnumerable<GetReturnRequestDto>> GetPendingReturnRequestsAsync(string userId)
        {
            _logger.LogInformation("Fetching pending return requests for user {UserId}", userId);

            try
            {
                string cacheKey = $"PendingReturnRequests_{userId}";
                var cachedRequests = _redisCacheService.GetData<IEnumerable<GetReturnRequestDto>>(cacheKey);

                if (cachedRequests != null)
                {
                    _logger.LogInformation("Retrieved {RequestCount} pending return requests from cache", cachedRequests.Count());
                    return cachedRequests;
                }

                var queryObject = new ReturnRequestQueryObject
                {
                    UserId = userId,
                    Status = ReturnStatus.Pending
                };

                var returnRequests = await _returnRequestRepository.GetAllReturnRequestsAsync(queryObject);
                var requestDtos = returnRequests.Select(MapToReturnRequestDto);

                _redisCacheService.SetData(cacheKey, requestDtos, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Cached {RequestCount} pending return requests", requestDtos.Count());

                return requestDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve pending return requests for user {UserId}", userId);
                throw;
            }
        }

        public async Task<IEnumerable<GetReturnRequestDto>> GetRecentReturnRequestsAsync(string userId, int daysBack = 7)
        {
            _logger.LogInformation("Fetching recent return requests for user {UserId} from last {Days} days", userId, daysBack);

            try
            {
                string cacheKey = $"RecentReturnRequests_{userId}_{daysBack}";
                var cachedRequests = _redisCacheService.GetData<IEnumerable<GetReturnRequestDto>>(cacheKey);

                if (cachedRequests != null)
                {
                    _logger.LogInformation("Retrieved {RequestCount} recent return requests from cache", cachedRequests.Count());
                    return cachedRequests;
                }

                var fromDate = DateTime.UtcNow.AddDays(-daysBack);
                var queryObject = new ReturnRequestQueryObject
                {
                    UserId = userId,
                    RequestDateFrom = fromDate
                };

                var returnRequests = await _returnRequestRepository.GetAllReturnRequestsAsync(queryObject);
                var requestDtos = returnRequests.Select(MapToReturnRequestDto);

                _redisCacheService.SetData(cacheKey, requestDtos, TimeSpan.FromMinutes(30));
                _logger.LogInformation("Cached {RequestCount} recent return requests", requestDtos.Count());

                return requestDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve recent return requests for user {UserId}", userId);
                throw;
            }
        }
    }
}