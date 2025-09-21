using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Book;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]

    public class BookController : ControllerBase
    {
        private readonly IBookService _bookService;
        private readonly ILogger<BookController> _logger;
        private readonly INotificationService _notifications;

        public BookController(IBookService bookService, ILogger<BookController> logger, INotificationService notifications)
        {
            _bookService = bookService;
            _logger = logger;
            _notifications = notifications;
        }

        private string GetId()
        {
            var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return id;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllBooks([FromQuery] BookQueryObject queryObject)
        {
            _logger.LogInformation("Processing GET request for all books from {RequestPath}", Request.Path);

            try
            {
                var books = await _bookService.GetAllBooksAsync(queryObject);
                if (books == null || !books.Any())
                {
                    _logger.LogInformation("No books found, returning empty list");
                    return Ok(new List<GetBookDto>());
                }

                _logger.LogInformation("Successfully returned {BookCount} books", books.Count());
                return Ok(books);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint}", Request.Path);
                return StatusCode(500, "An error occurred while fetching books.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBookById(int id)
        {
            _logger.LogInformation("Processing GET request for book {BookId} from {RequestPath}", id, Request.Path);

            try
            {
                var book = await _bookService.GetBookByIdAsync(id);
                if (book == null)
                {
                    _logger.LogInformation("Book {BookId} not found, returning 404", id);
                    return NotFound($"Book with ID {id} not found.");
                }

                _logger.LogInformation("Successfully returned book {BookId}", id);
                return Ok(book);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with BookId {BookId}", Request.Path, id);
                return StatusCode(500, "An error occurred while fetching the book.");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateBook([FromBody] CreateBookDto bookDto)
        {
            _logger.LogInformation("Processing POST request to create book from {RequestPath}", Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for book creation: {@ValidationErrors}",
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var book = await _bookService.CreateBookAsync(bookDto);

                if (book == null)
                {
                    _logger.LogWarning("Book creation failed - service returned null result");
                    return BadRequest("Failed to create book.");
                }

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully created a book!");
                }

                _logger.LogInformation("Successfully created book with ID {BookId}", book.Id);
                return CreatedAtAction(nameof(GetBookById), new { id = book.Id }, book);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Book creation failed - referenced entity not found: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} during book creation", Request.Path);
                return StatusCode(500, "An error occurred while creating the book.");
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateBook(int id, [FromBody] UpdateBookDto bookDto)
        {
            _logger.LogInformation("Processing PUT request for book {BookId} from {RequestPath}", id, Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for book {BookId} update: {@ValidationErrors}", id,
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var updatedBook = await _bookService.UpdateBookAsync(id, bookDto);

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully updated a book!");
                }

                _logger.LogInformation("Successfully updated book {BookId}", id);
                return Ok(updatedBook);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogInformation("Book {BookId} not found for update: {Message}", id, ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with BookId {BookId}", Request.Path, id);
                return StatusCode(500, "An error occurred while updating the book.");
            }
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            _logger.LogInformation("Processing DELETE request for book {BookId} from {RequestPath}", id, Request.Path);

            try
            {
                var result = await _bookService.DeleteBookAsync(id);
                if (!result)
                {
                    _logger.LogInformation("Book {BookId} not found for deletion", id);
                    return NotFound($"Book with ID {id} not found.");
                }

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully deleted a book!");
                }

                _logger.LogInformation("Successfully deleted book {BookId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with BookId {BookId}", Request.Path, id);
                return StatusCode(500, "An error occurred while deleting the book.");
            }
        }



    }
}