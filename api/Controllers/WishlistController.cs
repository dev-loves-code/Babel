using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Dtos.Wishlist;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(IWishlistService wishlistService, ILogger<WishlistController> logger)
        {
            _wishlistService = wishlistService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<GetWishlistDto>>> GetMyWishlist([FromQuery] WishlistQueryObject queryObject)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var wishlist = await _wishlistService.GetUserWishlistAsync(userId, queryObject);
                return Ok(wishlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user wishlist");
                return StatusCode(500, "Internal server error occurred while retrieving your wishlist");
            }
        }

        [HttpGet("is-in-wishlist/{bookId}")]
        public async Task<IActionResult> IsBookInWishlist(int bookId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                bool isInWishlist = await _wishlistService.IsBookInWishlistAsync(userId, bookId);
                return Ok(isInWishlist);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if book {BookId} is in wishlist for user {UserId}", bookId, User?.Identity?.Name);
                return StatusCode(500, "Internal server error occurred while checking your wishlist");
            }
        }


        [HttpPost]
        public async Task<ActionResult<GetWishlistDto>> AddToWishlist([FromBody] AddToWishlistDto wishlistDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var wishlistItem = await _wishlistService.AddToWishlistAsync(userId, wishlistDto);
                return CreatedAtAction(nameof(GetMyWishlist), wishlistItem);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Book not found when adding to wishlist");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when adding to wishlist");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding book to wishlist");
                return StatusCode(500, "Internal server error occurred while adding book to wishlist");
            }
        }

        [HttpDelete("book/{bookId:int}")]
        public async Task<ActionResult> RemoveFromWishlist(int bookId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var result = await _wishlistService.RemoveFromWishlistAsync(userId, bookId);

                if (!result)
                    return NotFound("Book not found in your wishlist");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing book {BookId} from wishlist", bookId);
                return StatusCode(500, "Internal server error occurred while removing book from wishlist");
            }
        }


    }
}