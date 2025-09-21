using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Book;
using api.Interfaces;
using api.Helpers;
using api.Models;
using Mapster;
using Microsoft.Extensions.Logging;

namespace api.Services
{
    public class BookService : IBookService
    {
        private readonly IBookRepository _bookRepository;
        private readonly IAuthorRepository _authorRepository;
        private readonly IGenreRepository _genreRepository;
        private readonly IRedisCacheService _redisCacheService;
        private readonly ILogger<BookService> _logger;

        private const string ALL_BOOKS_CACHE_KEY_PREFIX = "AllBooks_";
        private const string BOOK_CACHE_KEY_PREFIX = "Book_";

        public BookService(IBookRepository bookRepository,
                           IAuthorRepository authorRepository,
                           IGenreRepository genreRepository,
                           IRedisCacheService redisCacheService,
                           ILogger<BookService> logger)
        {
            _bookRepository = bookRepository;
            _authorRepository = authorRepository;
            _genreRepository = genreRepository;
            _redisCacheService = redisCacheService;
            _logger = logger;
        }

        private string BuildCacheKey(BookQueryObject queryObject)
        {
            var parts = new List<string>
            {
                ALL_BOOKS_CACHE_KEY_PREFIX
            };

            if (!string.IsNullOrEmpty(queryObject.Title))
                parts.Add($"title:{queryObject.Title.ToLower()}");

            if (!string.IsNullOrEmpty(queryObject.GenreName))
                parts.Add($"genre:{queryObject.GenreName.ToLower()}");

            if (!string.IsNullOrEmpty(queryObject.AuthorName))
                parts.Add($"author:{queryObject.AuthorName.ToLower()}");

            if (queryObject.MinQuantity.HasValue)
                parts.Add($"minQty:{queryObject.MinQuantity.Value}");

            if (queryObject.MaxQuantity.HasValue)
                parts.Add($"maxQty:{queryObject.MaxQuantity.Value}");

            return string.Join(":", parts);
        }

        public async Task<IEnumerable<GetBookDto>> GetAllBooksAsync(BookQueryObject queryObject)
        {
            _logger.LogInformation("Starting to fetch all books");

            try
            {
                string cacheKey = BuildCacheKey(queryObject);
                var cachedBooks = _redisCacheService.GetData<IEnumerable<GetBookDto>>(cacheKey);
                if (cachedBooks != null)
                {
                    _logger.LogInformation("Retrieved {BookCount} books from cache", cachedBooks.Count());
                    return cachedBooks;
                }

                _logger.LogInformation("Cache miss - fetching books from repository");
                var books = await _bookRepository.GetAllBooksAsync(queryObject);
                var bookDtos = books.Select(b => new GetBookDto
                {
                    Id = b.Id,
                    Title = b.Title,
                    Quantity = b.Quantity,
                    GenreId = b.GenreId,
                    GenreName = b.Genre?.Name ?? string.Empty,
                    AuthorId = b.AuthorId,
                    AuthorName = b.Author?.Name ?? string.Empty
                });

                if (bookDtos.Any())
                {
                    _redisCacheService.SetData(cacheKey, bookDtos);
                    _logger.LogInformation("Cached {BookCount} books", bookDtos.Count());
                }

                return bookDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve all books");
                throw;
            }
        }

        public async Task<GetBookDto?> GetBookByIdAsync(int id)
        {
            _logger.LogInformation("Fetching book with ID {BookId}", id);

            try
            {
                string cacheKey = $"{BOOK_CACHE_KEY_PREFIX}{id}";
                var cachedBook = _redisCacheService.GetData<GetBookDto>(cacheKey);

                if (cachedBook != null)
                {
                    _logger.LogInformation("Retrieved book {BookId} from cache", id);
                    return cachedBook;
                }

                _logger.LogInformation("Cache miss - fetching book {BookId} from repository", id);
                var book = await _bookRepository.GetBookByIdAsync(id);

                if (book == null)
                {
                    _logger.LogWarning("Book {BookId} not found", id);
                    return null;
                }

                var bookDto = new GetBookDto
                {
                    Id = book.Id,
                    Title = book.Title,
                    Quantity = book.Quantity,
                    GenreId = book.GenreId,
                    GenreName = book.Genre?.Name ?? string.Empty,
                    AuthorId = book.AuthorId,
                    AuthorName = book.Author?.Name ?? string.Empty
                };

                _redisCacheService.SetData(cacheKey, bookDto);
                _logger.LogInformation("Cached book {BookId}", id);

                return bookDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve book {BookId}", id);
                throw;
            }
        }

        public async Task<GetBookDto> CreateBookAsync(CreateBookDto bookDto)
        {
            _logger.LogInformation("Creating new book with title: {BookTitle}", bookDto.Title);

            try
            {
                // Validate that author and genre exist
                if (!await _authorRepository.AuthorExistsAsync(bookDto.AuthorId))
                {
                    throw new KeyNotFoundException($"Author with ID {bookDto.AuthorId} not found");
                }

                if (!await _genreRepository.GenreExistsAsync(bookDto.GenreId))
                {
                    throw new KeyNotFoundException($"Genre with ID {bookDto.GenreId} not found");
                }

                var book = bookDto.Adapt<Book>();
                var bookCreated = await _bookRepository.CreateBookAsync(book);

                if (bookCreated == null)
                {
                    _logger.LogError("Repository returned null when creating book");
                    throw new InvalidOperationException("Failed to create book");
                }

                // Get the complete book with navigation properties
                var completeBook = await _bookRepository.GetBookByIdAsync(bookCreated.Id);

                var resultDto = new GetBookDto
                {
                    Id = completeBook.Id,
                    Title = completeBook.Title,
                    Quantity = completeBook.Quantity,
                    GenreId = completeBook.GenreId,
                    GenreName = completeBook.Genre?.Name ?? string.Empty,
                    AuthorId = completeBook.AuthorId,
                    AuthorName = completeBook.Author?.Name ?? string.Empty
                };

                // Invalidate all books cache
                string allBooksCacheKey = ALL_BOOKS_CACHE_KEY_PREFIX;
                _redisCacheService.RemoveData(allBooksCacheKey);
                _logger.LogInformation("Invalidated all books cache after creating book {BookId}", bookCreated.Id);

                // Cache the new book
                string bookCacheKey = $"{BOOK_CACHE_KEY_PREFIX}{bookCreated.Id}";
                _redisCacheService.SetData(bookCacheKey, resultDto);

                _logger.LogInformation("Successfully created book {BookId} with title: {BookTitle}", bookCreated.Id, bookDto.Title);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create book");
                throw;
            }
        }

        public async Task<GetBookDto> UpdateBookAsync(int id, UpdateBookDto bookDto)
        {
            _logger.LogInformation("Updating book {BookId}", id);

            try
            {
                var existingBook = await _bookRepository.GetBookByIdAsync(id);
                if (existingBook == null)
                {
                    _logger.LogWarning("Book {BookId} not found", id);
                    throw new KeyNotFoundException($"Book with ID {id} not found");
                }

                // Validate that author and genre exist
                if (!await _authorRepository.AuthorExistsAsync(bookDto.AuthorId))
                {
                    throw new KeyNotFoundException($"Author with ID {bookDto.AuthorId} not found");
                }

                if (!await _genreRepository.GenreExistsAsync(bookDto.GenreId))
                {
                    throw new KeyNotFoundException($"Genre with ID {bookDto.GenreId} not found");
                }

                _logger.LogInformation("Updating book {BookId} - Title: '{OldTitle}' -> '{NewTitle}'",
                    id, existingBook.Title, bookDto.Title);

                existingBook.Title = bookDto.Title;
                existingBook.Quantity = bookDto.Quantity;
                existingBook.GenreId = bookDto.GenreId;
                existingBook.AuthorId = bookDto.AuthorId;

                var updatedBook = await _bookRepository.UpdateBookAsync(existingBook);

                // Get the complete book with updated navigation properties
                var completeBook = await _bookRepository.GetBookByIdAsync(updatedBook.Id);

                var resultDto = new GetBookDto
                {
                    Id = completeBook.Id,
                    Title = completeBook.Title,
                    Quantity = completeBook.Quantity,
                    GenreId = completeBook.GenreId,
                    GenreName = completeBook.Genre?.Name ?? string.Empty,
                    AuthorId = completeBook.AuthorId,
                    AuthorName = completeBook.Author?.Name ?? string.Empty
                };

                await InvalidateBookCacheAsync(id);
                _logger.LogInformation("Successfully updated book {BookId}", id);

                return resultDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update book {BookId}", id);
                throw;
            }
        }

        public async Task<bool> DeleteBookAsync(int id)
        {
            _logger.LogInformation("Deleting book {BookId}", id);

            try
            {
                var exists = await _bookRepository.BookExistsAsync(id);
                if (!exists)
                {
                    _logger.LogWarning("Cannot delete book {BookId} - book does not exist", id);
                    return false;
                }

                var result = await _bookRepository.DeleteBookAsync(id);

                if (result)
                {
                    await InvalidateBookCacheAsync(id);
                    _logger.LogInformation("Successfully deleted book {BookId}", id);
                }
                else
                {
                    _logger.LogWarning("Failed to delete book {BookId} - repository operation failed", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete book {BookId}", id);
                throw;
            }
        }

        public async Task<bool> BookExistsAsync(int id)
        {
            _logger.LogDebug("Checking if book {BookId} exists", id);

            try
            {
                string cacheKey = $"{BOOK_CACHE_KEY_PREFIX}{id}";
                var cachedBook = _redisCacheService.GetData<GetBookDto>(cacheKey);

                if (cachedBook != null)
                {
                    _logger.LogDebug("Book {BookId} exists (found in cache)", id);
                    return true;
                }

                var exists = await _bookRepository.BookExistsAsync(id);
                _logger.LogDebug("Book {BookId} exists: {Exists}", id, exists);
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if book {BookId} exists", id);
                throw;
            }
        }




        private async System.Threading.Tasks.Task InvalidateBookCacheAsync(int bookId)
        {
            try
            {
                // Remove specific book cache
                string bookCacheKey = $"{BOOK_CACHE_KEY_PREFIX}{bookId}";
                _redisCacheService.RemoveData(bookCacheKey);

                // Remove all books cache
                string allBooksCacheKey = ALL_BOOKS_CACHE_KEY_PREFIX;
                _redisCacheService.RemoveData(allBooksCacheKey);



                _logger.LogDebug("Invalidated cache for book {BookId} and related caches", bookId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to invalidate cache for book {BookId}", bookId);
            }
        }
    }
}