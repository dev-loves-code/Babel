using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Dtos.Borrow;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BorrowController : ControllerBase
    {
        private readonly IBorrowService _borrowService;
        private readonly ILogger<BorrowController> _logger;
        private readonly INotificationService _notifications;

        public BorrowController(IBorrowService borrowService, ILogger<BorrowController> logger, INotificationService notifications)
        {
            _borrowService = borrowService;
            _logger = logger;
            _notifications = notifications;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<GetBorrowDto>>> GetAllBorrows([FromQuery] BorrowQueryObject queryObject)
        {
            try
            {
                var borrows = await _borrowService.GetAllBorrowsAsync(queryObject);
                return Ok(borrows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all borrows");
                return StatusCode(500, "Internal server error occurred while retrieving borrows");
            }
        }

        [HttpGet("{bookId:int}")]
        public async Task<ActionResult<GetBorrowDto>> GetBorrowByBook(int bookId)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserId)) return Unauthorized();

                var borrow = await _borrowService.GetBorrowByBookAndUserAsync(bookId, currentUserId);

                if (borrow == null)
                    return NotFound($"You have not borrowed the book with ID {bookId}");

                return Ok(borrow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving borrow for Book ID {BookId}", bookId);
                return StatusCode(500, "Internal server error occurred while retrieving borrow");
            }
        }


        [HttpGet("my-borrows")]
        public async Task<ActionResult<IEnumerable<GetBorrowDto>>> GetMyBorrows()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var borrows = await _borrowService.GetUserBorrowsAsync(userId);
                return Ok(borrows);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user borrows");
                return StatusCode(500, "Internal server error occurred while retrieving your borrows");
            }
        }

        [HttpPost]
        public async Task<ActionResult<GetBorrowDto>> CreateBorrow([FromBody] CreateBorrowDto borrowDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var borrow = await _borrowService.CreateBorrowAsync(userId, borrowDto);


                await _notifications.SendPrivateMessageAsync(userId, $"You have successfully borrowed the book with ID {borrow.BookId}.");
                return CreatedAtAction(nameof(GetBorrowByBook), new { bookId = borrow.BookId }, borrow);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found when creating borrow");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating borrow");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating borrow");
                return StatusCode(500, "Internal server error occurred while creating borrow");
            }
        }

        [HttpPost("{id:int}/complete")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<GetBorrowDto>> CompleteBorrow(int id)
        {
            try
            {
                var borrow = await _borrowService.CompleteBorrowAsync(id);
                return Ok(borrow);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Borrow not found when completing");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when completing borrow");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing borrow {BorrowId}", id);
                return StatusCode(500, "Internal server error occurred while completing borrow");
            }
        }

        [HttpGet("can-borrow")]
        public async Task<ActionResult<bool>> CanUserBorrow()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var canBorrow = await _borrowService.CanUserBorrowAsync(userId);
                return Ok(canBorrow);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user can borrow");
                return StatusCode(500, "Internal server error occurred while checking borrow eligibility");
            }
        }

        [HttpGet("book/{bookId:int}/availability")]
        public async Task<ActionResult<bool>> CheckBookAvailability(int bookId)
        {
            try
            {
                var isAvailable = await _borrowService.IsBookAvailableAsync(bookId);
                return Ok(isAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking book {BookId} availability", bookId);
                return StatusCode(500, "Internal server error occurred while checking book availability");
            }
        }
    }

}