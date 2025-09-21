using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Genre;
using api.Interfaces;
using api.Helpers;
using api.Models;
using Mapster;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class GenreService : IGenreService
    {
        private readonly IGenreRepository _genreRepository;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<GenreService> _logger;

        private const string ALL_GENRES_CACHE_KEY_PREFIX = "AllGenres_";
        private const string GENRE_CACHE_KEY_PREFIX = "Genre_";

        private string BuildCacheKey(GenreQueryObject queryObject)
        {
            var parts = new List<string>
            {
                ALL_GENRES_CACHE_KEY_PREFIX
            };

            if (!string.IsNullOrEmpty(queryObject.Name))
                parts.Add($"name:{queryObject.Name.ToLower()}");

            return string.Join(":", parts);
        }

        public GenreService(
            IGenreRepository genreRepository,
            IRedisCacheService redisCacheService,
            ILogger<GenreService> logger)
        {
            _genreRepository = genreRepository;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetGenreDto>> GetAllGenresAsync(GenreQueryObject queryObject)
        {
            _logger.LogInformation("Starting to fetch all genres");

            try
            {
                string cacheKey = BuildCacheKey(queryObject);
                var cachedGenres = _redisCacheService.GetData<IEnumerable<GetGenreDto>>(cacheKey);
                if (cachedGenres != null)
                {
                    _logger.LogInformation("Retrieved {GenreCount} genres from cache", cachedGenres.Count());
                    return cachedGenres;
                }

                _logger.LogInformation("Cache miss - fetching genres from repository");
                var genres = await _genreRepository.GetAllGenresAsync(queryObject);
                var genreDtos = genres.Select(g => new GetGenreDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Books = g.Books?.Select(b => new BookSummaryDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Quantity = b.Quantity,
                        AuthorName = b.Author?.Name ?? string.Empty
                    }).ToList()
                });

                if (genreDtos.Any())
                {
                    _redisCacheService.SetData(cacheKey, genreDtos);
                    _logger.LogInformation("Cached {GenreCount} genres", genreDtos.Count());
                }

                return genreDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all genres");
                throw;
            }
        }

        public async Task<GetGenreDto?> GetGenreByIdAsync(int id)
        {
            _logger.LogInformation("Fetching genre with ID {GenreId}", id);

            try
            {
                string cacheKey = $"{GENRE_CACHE_KEY_PREFIX}{id}";
                var cachedGenre = _redisCacheService.GetData<GetGenreDto>(cacheKey);

                if (cachedGenre != null)
                {
                    _logger.LogInformation("Retrieved genre {GenreId} from cache", id);
                    return cachedGenre;
                }

                _logger.LogInformation("Cache miss - fetching genre {GenreId} from repository", id);
                var genre = await _genreRepository.GetGenreByIdAsync(id);

                if (genre == null)
                {
                    _logger.LogWarning("Genre {GenreId} not found", id);
                    return null;
                }

                var genreDto = new GetGenreDto
                {
                    Id = genre.Id,
                    Name = genre.Name,
                    Books = genre.Books?.Select(b => new BookSummaryDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Quantity = b.Quantity,
                        AuthorName = b.Author?.Name ?? string.Empty
                    }).ToList()
                };

                _redisCacheService.SetData(cacheKey, genreDto);
                _logger.LogInformation("Cached genre {GenreId}", id);

                return genreDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve genre {GenreId}", id);
                throw;
            }
        }

        public async Task<GetGenreDto> CreateGenreAsync(CreateGenreDto genreDto)
        {
            _logger.LogInformation("Creating new genre with name: {GenreName}", genreDto.Name);

            try
            {
                var genre = genreDto.Adapt<Genre>();
                var genreCreated = await _genreRepository.CreateGenreAsync(genre);

                if (genreCreated == null)
                {
                    _logger.LogError("Repository returned null when creating genre");
                    throw new InvalidOperationException("Failed to create genre");
                }

                var resultDto = new GetGenreDto
                {
                    Id = genreCreated.Id,
                    Name = genreCreated.Name,
                    Books = new List<BookSummaryDto>()
                };

                // Invalidate all genres cache
                string allGenresCacheKey = ALL_GENRES_CACHE_KEY_PREFIX;
                _redisCacheService.RemoveData(allGenresCacheKey);
                _logger.LogInformation("Invalidated all genres cache after creating genre {GenreId}", genreCreated.Id);

                // Cache the new genre
                string genreCacheKey = $"{GENRE_CACHE_KEY_PREFIX}{genreCreated.Id}";
                _redisCacheService.SetData(genreCacheKey, resultDto);

                _logger.LogInformation("Successfully created genre {GenreId} with name: {GenreName}", genreCreated.Id, genreDto.Name);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create genre");
                throw;
            }
        }

        public async Task<GetGenreDto> UpdateGenreAsync(int id, UpdateGenreDto genreDto)
        {
            _logger.LogInformation("Updating genre {GenreId}", id);

            try
            {
                var existingGenre = await _genreRepository.GetGenreByIdAsync(id);
                if (existingGenre == null)
                {
                    _logger.LogWarning("Genre {GenreId} not found", id);
                    throw new KeyNotFoundException($"Genre with ID {id} not found");
                }

                _logger.LogInformation("Updating genre {GenreId} - Name: '{OldName}' -> '{NewName}'",
                    id, existingGenre.Name, genreDto.Name);

                existingGenre.Name = genreDto.Name;

                var updatedGenre = await _genreRepository.UpdateGenreAsync(existingGenre);

                // Get the complete genre with updated navigation properties
                var completeGenre = await _genreRepository.GetGenreByIdAsync(updatedGenre.Id);

                var resultDto = new GetGenreDto
                {
                    Id = completeGenre.Id,
                    Name = completeGenre.Name,
                    Books = completeGenre.Books?.Select(b => new BookSummaryDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Quantity = b.Quantity,
                        AuthorName = b.Author?.Name ?? string.Empty
                    }).ToList()
                };

                await InvalidateGenreCacheAsync(id);
                _logger.LogInformation("Successfully updated genre {GenreId}", id);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update genre {GenreId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteGenreAsync(int id)
        {
            _logger.LogInformation("Deleting genre {GenreId}", id);

            try
            {
                // Check if genre has books
                if (await _genreRepository.GenreHasBooksAsync(id))
                {
                    _logger.LogWarning("Cannot delete genre {GenreId} - genre has books", id);
                    throw new InvalidOperationException("Cannot delete genre with existing books. Please reassign or remove books first.");
                }

                var exists = await _genreRepository.GenreExistsAsync(id);
                if (!exists)
                {
                    _logger.LogWarning("Cannot delete genre {GenreId} - genre does not exist", id);
                    return false;
                }

                var result = await _genreRepository.DeleteGenreAsync(id);

                if (result)
                {
                    await InvalidateGenreCacheAsync(id);
                    _logger.LogInformation("Successfully deleted genre {GenreId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete genre {GenreId} - repository operation failed", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete genre {GenreId}", id);
                throw;
            }
        }

        public async Task<bool> GenreExistsAsync(int id)
        {
            _logger.LogDebug("Checking if genre {GenreId} exists", id);

            try
            {
                string cacheKey = $"{GENRE_CACHE_KEY_PREFIX}{id}";
                var cachedGenre = _redisCacheService.GetData<GetGenreDto>(cacheKey);

                if (cachedGenre != null)
                {
                    _logger.LogDebug("Genre {GenreId} exists (found in cache)", id);
                    return true;
                }

                var exists = await _genreRepository.GenreExistsAsync(id);
                _logger.LogDebug("Genre {GenreId} exists: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if genre {GenreId} exists", id);
                throw;
            }
        }

        private async System.Threading.Tasks.Task InvalidateGenreCacheAsync(int genreId)
        {
            try
            {
                // Remove specific genre cache
                string genreCacheKey = $"{GENRE_CACHE_KEY_PREFIX}{genreId}";
                _redisCacheService.RemoveData(genreCacheKey);

                // Remove all genres cache
                string allGenresCacheKey = ALL_GENRES_CACHE_KEY_PREFIX;
                _redisCacheService.RemoveData(allGenresCacheKey);

                // Also invalidate book-related caches since genre names appear in book DTOs
                _redisCacheService.RemoveData("AllBooks_");
                _redisCacheService.RemoveData($"BooksByGenre_{genreId}");

                _logger.LogDebug("Invalidated cache for genre {GenreId} and related caches", genreId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for genre {GenreId}", genreId);
            }
        }
    }
}