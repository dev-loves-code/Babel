using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Helpers;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories
{
    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDBContext _context;

        public BookRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Book>> GetAllBooksAsync(BookQueryObject queryObject)
        {
            var query = _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genre)
                .Include(b => b.Borrows)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryObject.Title))
            {
                query = query.Where(b => b.Title.Contains(queryObject.Title));
            }

            if (!string.IsNullOrWhiteSpace(queryObject.GenreName))
            {
                query = query.Where(b => b.Genre.Name.Contains(queryObject.GenreName));
            }

            if (!string.IsNullOrWhiteSpace(queryObject.AuthorName))
            {
                query = query.Where(b => b.Author.Name.Contains(queryObject.AuthorName));
            }

            if (queryObject.MinQuantity.HasValue)
            {
                query = query.Where(b => b.Quantity >= queryObject.MinQuantity.Value);
            }

            if (queryObject.MaxQuantity.HasValue)
            {
                query = query.Where(b => b.Quantity <= queryObject.MaxQuantity.Value);
            }

            return await query.ToListAsync();
        }

        public async Task<Book?> GetBookByIdAsync(int id)
        {
            return await _context.Books
                .Include(b => b.Author)
                .Include(b => b.Genre)
                .Include(b => b.Borrows)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Book> CreateBookAsync(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
            return book;
        }

        public async Task<Book> UpdateBookAsync(Book book)
        {
            _context.Books.Update(book);
            await _context.SaveChangesAsync();
            return book;
        }

        public async Task<bool> DeleteBookAsync(int id)
        {
            var book = await GetBookByIdAsync(id);
            if (book == null) return false;

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> BookExistsAsync(int id)
        {
            return await _context.Books.AnyAsync(b => b.Id == id);
        }

    }
}