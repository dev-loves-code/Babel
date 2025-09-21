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
    public interface IGenreService
    {
        Task<IEnumerable<GetGenreDto>> GetAllGenresAsync(GenreQueryObject queryObject);
        Task<GetGenreDto?> GetGenreByIdAsync(int id);
        Task<GetGenreDto> CreateGenreAsync(CreateGenreDto genreDto);
        Task<GetGenreDto> UpdateGenreAsync(int id, UpdateGenreDto genreDto);
        Task<bool> DeleteGenreAsync(int id);
        Task<bool> GenreExistsAsync(int id);
    }
}