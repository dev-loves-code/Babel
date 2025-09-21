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
    public interface IAuthorRepository
    {
        Task<IEnumerable<Author>> GetAllAuthorsAsync(AuthorQueryObject queryObject);
        Task<Author?> GetAuthorByIdAsync(int id);
        Task<Author> CreateAuthorAsync(Author author);
        Task<Author> UpdateAuthorAsync(Author author);
        Task<bool> DeleteAuthorAsync(int id);
        Task<bool> AuthorExistsAsync(int id);
        Task<bool> AuthorHasBooksAsync(int id);
    }
}