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
    public interface IBookRepository
    {
        Task<IEnumerable<Book>> GetAllBooksAsync(BookQueryObject queryObject);
        Task<Book?> GetBookByIdAsync(int id);
        Task<Book> CreateBookAsync(Book book);
        Task<Book> UpdateBookAsync(Book book);
        Task<bool> DeleteBookAsync(int id);
        Task<bool> BookExistsAsync(int id);
    }
}