using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Book;
using api.Dtos.Author;
using api.Dtos.Genre;
using api.Helpers;
using api.Models;

namespace api.Interfaces
{
    public interface IGenreRepository
    {
        Task<IEnumerable<Genre>> GetAllGenresAsync(GenreQueryObject queryObject);
        Task<Genre?> GetGenreByIdAsync(int id);
        Task<Genre> CreateGenreAsync(Genre genre);
        Task<Genre> UpdateGenreAsync(Genre genre);
        Task<bool> DeleteGenreAsync(int id);
        Task<bool> GenreExistsAsync(int id);
        Task<bool> GenreHasBooksAsync(int id);
    }
}