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
    public interface IBookService
    {
        Task<IEnumerable<GetBookDto>> GetAllBooksAsync(BookQueryObject queryObject);
        Task<GetBookDto?> GetBookByIdAsync(int id);
        Task<GetBookDto> CreateBookAsync(CreateBookDto bookDto);
        Task<GetBookDto> UpdateBookAsync(int id, UpdateBookDto bookDto);
        Task<bool> DeleteBookAsync(int id);
        Task<bool> BookExistsAsync(int id);
    }
}