using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Genre;
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
    public class GenreController : ControllerBase
    {
        private readonly IGenreService _genreService;
        private readonly ILogger<GenreController> _logger;
        private readonly INotificationService _notifications;

        public GenreController(IGenreService genreService, ILogger<GenreController> logger, INotificationService notifications)
        {
            _genreService = genreService;
            _logger = logger;
            _notifications = notifications;
        }

        private string GetId()
        {
            var id = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return id;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllGenres([FromQuery] GenreQueryObject queryObject)
        {
            _logger.LogInformation("Processing GET request for all genres from {RequestPath}", Request.Path);

            try
            {
                var genres = await _genreService.GetAllGenresAsync(queryObject);
                if (genres == null || !genres.Any())
                {
                    _logger.LogInformation("No genres found, returning empty list");
                    return Ok(new List<GetGenreDto>());
                }

                _logger.LogInformation("Successfully returned {GenreCount} genres", genres.Count());
                return Ok(genres);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint}", Request.Path);
                return StatusCode(500, "An error occurred while fetching genres.");
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetGenreById(int id)
        {
            _logger.LogInformation("Processing GET request for genre {GenreId} from {RequestPath}", id, Request.Path);

            try
            {
                var genre = await _genreService.GetGenreByIdAsync(id);
                if (genre == null)
                {
                    _logger.LogInformation("Genre {GenreId} not found, returning 404", id);
                    return NotFound($"Genre with ID {id} not found.");
                }

                _logger.LogInformation("Successfully returned genre {GenreId}", id);
                return Ok(genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with GenreId {GenreId}", Request.Path, id);
                return StatusCode(500, "An error occurred while fetching the genre.");
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateGenre([FromBody] CreateGenreDto genreDto)
        {
            _logger.LogInformation("Processing POST request to create genre from {RequestPath}", Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for genre creation: {@ValidationErrors}",
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var genre = await _genreService.CreateGenreAsync(genreDto);

                if (genre == null)
                {
                    _logger.LogWarning("Genre creation failed - service returned null result");
                    return BadRequest("Failed to create genre.");
                }

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully created a genre!");
                }

                _logger.LogInformation("Successfully created genre with ID {GenreId}", genre.Id);
                return CreatedAtAction(nameof(GetGenreById), new { id = genre.Id }, genre);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} during genre creation", Request.Path);
                return StatusCode(500, "An error occurred while creating the genre.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateGenre(int id, [FromBody] UpdateGenreDto genreDto)
        {
            _logger.LogInformation("Processing PUT request for genre {GenreId} from {RequestPath}", id, Request.Path);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed for genre {GenreId} update: {@ValidationErrors}", id,
                    ModelState.Where(x => x.Value.Errors.Count > 0).ToDictionary(k => k.Key, v => v.Value.Errors));
                return BadRequest(ModelState);
            }

            try
            {
                var updatedGenre = await _genreService.UpdateGenreAsync(id, genreDto);

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully updated a genre!");
                }

                _logger.LogInformation("Successfully updated genre {GenreId}", id);
                return Ok(updatedGenre);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogInformation("Genre {GenreId} not found for update: {Message}", id, ex.Message);
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with GenreId {GenreId}", Request.Path, id);
                return StatusCode(500, "An error occurred while updating the genre.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteGenre(int id)
        {
            _logger.LogInformation("Processing DELETE request for genre {GenreId} from {RequestPath}", id, Request.Path);

            try
            {
                var result = await _genreService.DeleteGenreAsync(id);
                if (!result)
                {
                    _logger.LogInformation("Genre {GenreId} not found for deletion", id);
                    return NotFound($"Genre with ID {id} not found.");
                }

                var userId = GetId();
                if (!string.IsNullOrEmpty(userId))
                {
                    await _notifications.SendPrivateMessageAsync(userId, "Successfully deleted a genre!");
                }

                _logger.LogInformation("Successfully deleted genre {GenreId}", id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Cannot delete genre {GenreId}: {Message}", id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HTTP request failed for endpoint {Endpoint} with GenreId {GenreId}", Request.Path, id);
                return StatusCode(500, "An error occurred while deleting the genre.");
            }
        }
    }
}