using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Borrow;
using api.Models;

namespace api.Interfaces
{
    public interface IBorrowRepository
    {
        Task<IEnumerable<Borrow>> GetAllBorrowsAsync(BorrowQueryObject queryObject);
        Task<Borrow?> GetBorrowByBookAndUserAsync(int bookId, string userId);
        Task<Borrow?> GetBorrowByIdAsync(int id);
        Task<IEnumerable<Borrow>> GetActiveBorrowsByUserAsync(string userId);
        Task<int> GetActiveBorrowCountByUserAsync(string userId);
        Task<int> GetActiveAndPastDueBorrowCountByBookAsync(int bookId);
        Task<Borrow> CreateBorrowAsync(Borrow borrow);
        Task<Borrow> UpdateBorrowAsync(Borrow borrow);
        Task<bool> BorrowExistsAsync(int id);
        Task<bool> HasActiveBorrowForBookAsync(string userId, int bookId);
    }
}