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
    public class GenreRepository : IGenreRepository
    {
        private readonly ApplicationDBContext _context;

        public GenreRepository(ApplicationDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Genre>> GetAllGenresAsync(GenreQueryObject queryObject)
        {
            var query = _context.Genres
                .Include(g => g.Books)
                    .ThenInclude(b => b.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(queryObject.Name))
            {
                query = query.Where(g => g.Name.Contains(queryObject.Name));
            }

            return await query.ToListAsync();
        }

        public async Task<Genre?> GetGenreByIdAsync(int id)
        {
            return await _context.Genres
                .Include(g => g.Books)
                    .ThenInclude(b => b.Author)
                .FirstOrDefaultAsync(g => g.Id == id);
        }

        public async Task<Genre> CreateGenreAsync(Genre genre)
        {
            _context.Genres.Add(genre);
            await _context.SaveChangesAsync();
            return genre;
        }

        public async Task<Genre> UpdateGenreAsync(Genre genre)
        {
            _context.Genres.Update(genre);
            await _context.SaveChangesAsync();
            return genre;
        }

        public async Task<bool> DeleteGenreAsync(int id)
        {
            var genre = await _context.Genres.FindAsync(id);
            if (genre == null) return false;

            _context.Genres.Remove(genre);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> GenreExistsAsync(int id)
        {
            return await _context.Genres.AnyAsync(g => g.Id == id);
        }

        public async Task<bool> GenreHasBooksAsync(int id)
        {
            return await _context.Books.AnyAsync(b => b.GenreId == id);
        }
    }
}