using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Author;
using api.Interfaces;
using api.Helpers;
using api.Models;
using Mapster;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class AuthorService : IAuthorService
    {
        private readonly IAuthorRepository _authorRepository;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<AuthorService> _logger;

        private const string ALL_AUTHORS_CACHE_KEY_PREFIX = "AllAuthors_";
        private const string AUTHOR_CACHE_KEY_PREFIX = "Author_";

        private string BuildCacheKey(AuthorQueryObject queryObject)
        {
            var parts = new List<string>
            {
                ALL_AUTHORS_CACHE_KEY_PREFIX
            };

            if (!string.IsNullOrEmpty(queryObject.Name))
                parts.Add($"name:{queryObject.Name.ToLower()}");

            return string.Join(":", parts);
        }

        public AuthorService(
            IAuthorRepository authorRepository,
            IRedisCacheService redisCacheService,
            ILogger<AuthorService> logger)
        {
            _authorRepository = authorRepository;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        public async Task<IEnumerable<GetAuthorDto>> GetAllAuthorsAsync(AuthorQueryObject queryObject)
        {
            _logger.LogInformation("Starting to fetch all authors");

            try
            {
                string cacheKey = BuildCacheKey(queryObject);
                var cachedAuthors = _redisCacheService.GetData<IEnumerable<GetAuthorDto>>(cacheKey);
                if (cachedAuthors != null)
                {
                    _logger.LogInformation("Retrieved {AuthorCount} authors from cache", cachedAuthors.Count());
                    return cachedAuthors;
                }

                _logger.LogInformation("Cache miss - fetching authors from repository");
                var authors = await _authorRepository.GetAllAuthorsAsync(queryObject);
                var authorDtos = authors.Select(a => new GetAuthorDto
                {
                    Id = a.Id,
                    Name = a.Name,
                    Books = a.Books?.Select(b => new BookSummaryDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Quantity = b.Quantity,
                        GenreName = b.Genre?.Name ?? string.Empty
                    }).ToList()
                });

                if (authorDtos.Any())
                {
                    _redisCacheService.SetData(cacheKey, authorDtos);
                    _logger.LogInformation("Cached {AuthorCount} authors", authorDtos.Count());
                }

                return authorDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all authors");
                throw;
            }
        }

        public async Task<GetAuthorDto?> GetAuthorByIdAsync(int id)
        {
            _logger.LogInformation("Fetching author with ID {AuthorId}", id);

            try
            {
                string cacheKey = $"{AUTHOR_CACHE_KEY_PREFIX}{id}";
                var cachedAuthor = _redisCacheService.GetData<GetAuthorDto>(cacheKey);

                if (cachedAuthor != null)
                {
                    _logger.LogInformation("Retrieved author {AuthorId} from cache", id);
                    return cachedAuthor;
                }

                _logger.LogInformation("Cache miss - fetching author {AuthorId} from repository", id);
                var author = await _authorRepository.GetAuthorByIdAsync(id);

                if (author == null)
                {
                    _logger.LogWarning("Author {AuthorId} not found", id);
                    return null;
                }

                var authorDto = new GetAuthorDto
                {
                    Id = author.Id,
                    Name = author.Name,
                    Books = author.Books?.Select(b => new BookSummaryDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Quantity = b.Quantity,
                        GenreName = b.Genre?.Name ?? string.Empty
                    }).ToList()
                };

                _redisCacheService.SetData(cacheKey, authorDto);
                _logger.LogInformation("Cached author {AuthorId}", id);

                return authorDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve author {AuthorId}", id);
                throw;
            }
        }

        public async Task<GetAuthorDto> CreateAuthorAsync(CreateAuthorDto authorDto)
        {
            _logger.LogInformation("Creating new author with name: {AuthorName}", authorDto.Name);

            try
            {
                var author = authorDto.Adapt<Author>();
                var authorCreated = await _authorRepository.CreateAuthorAsync(author);

                if (authorCreated == null)
                {
                    _logger.LogError("Repository returned null when creating author");
                    throw new InvalidOperationException("Failed to create author");
                }

                var resultDto = new GetAuthorDto
                {
                    Id = authorCreated.Id,
                    Name = authorCreated.Name,
                    Books = new List<BookSummaryDto>()
                };

                // Invalidate all authors cache
                string allAuthorsCacheKey = ALL_AUTHORS_CACHE_KEY_PREFIX;
                _redisCacheService.RemoveData(allAuthorsCacheKey);
                _logger.LogInformation("Invalidated all authors cache after creating author {AuthorId}", authorCreated.Id);

                // Cache the new author
                string authorCacheKey = $"{AUTHOR_CACHE_KEY_PREFIX}{authorCreated.Id}";
                _redisCacheService.SetData(authorCacheKey, resultDto);

                _logger.LogInformation("Successfully created author {AuthorId} with name: {AuthorName}", authorCreated.Id, authorDto.Name);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create author");
                throw;
            }
        }

        public async Task<GetAuthorDto> UpdateAuthorAsync(int id, UpdateAuthorDto authorDto)
        {
            _logger.LogInformation("Updating author {AuthorId}", id);

            try
            {
                var existingAuthor = await _authorRepository.GetAuthorByIdAsync(id);
                if (existingAuthor == null)
                {
                    _logger.LogWarning("Author {AuthorId} not found", id);
                    throw new KeyNotFoundException($"Author with ID {id} not found");
                }

                _logger.LogInformation("Updating author {AuthorId} - Name: '{OldName}' -> '{NewName}'",
                    id, existingAuthor.Name, authorDto.Name);

                existingAuthor.Name = authorDto.Name;

                var updatedAuthor = await _authorRepository.UpdateAuthorAsync(existingAuthor);

                // Get the complete author with updated navigation properties
                var completeAuthor = await _authorRepository.GetAuthorByIdAsync(updatedAuthor.Id);

                var resultDto = new GetAuthorDto
                {
                    Id = completeAuthor.Id,
                    Name = completeAuthor.Name,
                    Books = completeAuthor.Books?.Select(b => new BookSummaryDto
                    {
                        Id = b.Id,
                        Title = b.Title,
                        Quantity = b.Quantity,
                        GenreName = b.Genre?.Name ?? string.Empty
                    }).ToList()
                };

                await InvalidateAuthorCacheAsync(id);
                _logger.LogInformation("Successfully updated author {AuthorId}", id);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update author {AuthorId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteAuthorAsync(int id)
        {
            _logger.LogInformation("Deleting author {AuthorId}", id);

            try
            {
                // Check if author has books
                if (await _authorRepository.AuthorHasBooksAsync(id))
                {
                    _logger.LogWarning("Cannot delete author {AuthorId} - author has books", id);
                    throw new InvalidOperationException("Cannot delete author with existing books. Please reassign or remove books first.");
                }

                var exists = await _authorRepository.AuthorExistsAsync(id);
                if (!exists)
                {
                    _logger.LogWarning("Cannot delete author {AuthorId} - author does not exist", id);
                    return false;
                }

                var result = await _authorRepository.DeleteAuthorAsync(id);

                if (result)
                {
                    await InvalidateAuthorCacheAsync(id);
                    _logger.LogInformation("Successfully deleted author {AuthorId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete author {AuthorId} - repository operation failed", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete author {AuthorId}", id);
                throw;
            }
        }

        public async Task<bool> AuthorExistsAsync(int id)
        {
            _logger.LogDebug("Checking if author {AuthorId} exists", id);

            try
            {
                string cacheKey = $"{AUTHOR_CACHE_KEY_PREFIX}{id}";
                var cachedAuthor = _redisCacheService.GetData<GetAuthorDto>(cacheKey);

                if (cachedAuthor != null)
                {
                    _logger.LogDebug("Author {AuthorId} exists (found in cache)", id);
                    return true;
                }

                var exists = await _authorRepository.AuthorExistsAsync(id);
                _logger.LogDebug("Author {AuthorId} exists: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if author {AuthorId} exists", id);
                throw;
            }
        }

        private async System.Threading.Tasks.Task InvalidateAuthorCacheAsync(int authorId)
        {
            try
            {
                // Remove specific author cache
                string authorCacheKey = $"{AUTHOR_CACHE_KEY_PREFIX}{authorId}";
                _redisCacheService.RemoveData(authorCacheKey);

                // Remove all authors cache
                string allAuthorsCacheKey = ALL_AUTHORS_CACHE_KEY_PREFIX;
                _redisCacheService.RemoveData(allAuthorsCacheKey);

                // Also invalidate book-related caches since author names appear in book DTOs
                _redisCacheService.RemoveData("AllBooks_");
                _redisCacheService.RemoveData($"BooksByAuthor_{authorId}");

                _logger.LogDebug("Invalidated cache for author {AuthorId} and related caches", authorId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for author {AuthorId}", authorId);
            }
        }
    }
}