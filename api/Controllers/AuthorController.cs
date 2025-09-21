using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Author;
using api.Helpers;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AuthorController : ControllerBase
    {
        private readonly IAuthorService _authorService;
        private readonly ILogger<AuthorController> _logger;
        private readonly INotificationService _notifications;

        public AuthorController(IAuthorService authorService, ILogger<AuthorController> logger, INotificationService notifications)
        {
            _authorService = authorService;
            _logger = logger;
            _notifications = notifications;
        }

        private string GetId()
        {
            var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return id;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAuthors([FromQuery] AuthorQueryObject queryObject)
        {
            _logger.LogInformation("Processing GET request for all authors from {RequestPath}", Request.Path);

            try
            {
                var authors = await _authorService.GetAllAuthorsAsync(queryObject);
                if (authors == null || !authors.Any())
                {
                    _logger.LogInformation("No authors found, returning empty list");
                    return Ok(new List<GetAuthorDto>());
                }

                _logger.LogInformation("Successfully returned {AuthorCount} authors", authors.Count());
                return Ok(authors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint}", Request.Path);
                return StatusCode(500, "An error occurred while fetching authors.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuthorById(int id)
        {
            _logger.LogInformation("Processing GET request for author {AuthorId} from {RequestPath}", id, Request.Path);

            try
            {
                var author = await _authorService.GetAuthorByIdAsync(id);
                if (author == null)
                {
                    _logger.LogInformation("Author {AuthorId} not found, returning 404", id);
                    return NotFound($"Author with ID {id} not found.");
                }

                _logger.LogInformation("Successfully returned author {AuthorId}", id);
                return Ok(author);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with AuthorId {AuthorId}", Request.Path, id);
                return StatusCode(500, "An error occurred while fetching the author.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAuthor([FromBody] CreateAuthorDto authorDto)
        {
            _logger.LogInformation("Processing POST request to create author from {RequestPath}", Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for author creation: {@ValidationErrors}",
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var author = await _authorService.CreateAuthorAsync(authorDto);

                if (author == null)
                {
                    _logger.LogWarning("Author creation failed - service returned null result");
                    return BadRequest("Failed to create author.");
                }

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully created an author!");
                }

                _logger.LogInformation("Successfully created author with ID {AuthorId}", author.Id);
                return CreatedAtAction(nameof(GetAuthorById), new { id = author.Id }, author);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} during author creation", Request.Path);
                return StatusCode(500, "An error occurred while creating the author.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuthor(int id, [FromBody] UpdateAuthorDto authorDto)
        {
            _logger.LogInformation("Processing PUT request for author {AuthorId} from {RequestPath}", id, Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for author {AuthorId} update: {@ValidationErrors}", id,
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var updatedAuthor = await _authorService.UpdateAuthorAsync(id, authorDto);

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully updated an author!");
                }

                _logger.LogInformation("Successfully updated author {AuthorId}", id);
                return Ok(updatedAuthor);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogInformation("Author {AuthorId} not found for update: {Message}", id, ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with AuthorId {AuthorId}", Request.Path, id);
                return StatusCode(500, "An error occurred while updating the author.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuthor(int id)
        {
            _logger.LogInformation("Processing DELETE request for author {AuthorId} from {RequestPath}", id, Request.Path);

            try
            {
                var result = await _authorService.DeleteAuthorAsync(id);
                if (!result)
                {
                    _logger.LogInformation("Author {AuthorId} not found for deletion", id);
                    return NotFound($"Author with ID {id} not found.");
                }

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully deleted an author!");
                }

                _logger.LogInformation("Successfully deleted author {AuthorId}", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot delete author {AuthorId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with AuthorId {AuthorId}", Request.Path, id);
                return StatusCode(500, "An error occurred while deleting the author.");
            }
        }
    }
}