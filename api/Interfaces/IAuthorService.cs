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
    public interface IAuthorService
    {
        Task<IEnumerable<GetAuthorDto>> GetAllAuthorsAsync(AuthorQueryObject queryObject);
        Task<GetAuthorDto?> GetAuthorByIdAsync(int id);
        Task<GetAuthorDto> CreateAuthorAsync(CreateAuthorDto authorDto);
        Task<GetAuthorDto> UpdateAuthorAsync(int id, UpdateAuthorDto authorDto);
        Task<bool> DeleteAuthorAsync(int id);
        Task<bool> AuthorExistsAsync(int id);
    }
}