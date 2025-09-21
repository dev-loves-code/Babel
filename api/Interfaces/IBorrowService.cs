using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Borrow;
using api.Dtos.ReturnRequest;

namespace api.Interfaces
{
    public interface IBorrowService
    {
        Task<IEnumerable<GetBorrowDto>> GetAllBorrowsAsync(BorrowQueryObject queryObject);
        Task<GetBorrowDto?> GetBorrowByBookAndUserAsync(int bookId, string userId);

        Task<IEnumerable<GetBorrowDto>> GetUserBorrowsAsync(string userId);
        Task<GetBorrowDto> CreateBorrowAsync(string userId, CreateBorrowDto borrowDto);
        Task<GetBorrowDto> CompleteBorrowAsync(int borrowId);
        Task<bool> CanUserBorrowAsync(string userId);
        Task<bool> IsBookAvailableAsync(int bookId);
        Task<IEnumerable<GetBorrowDto>> GetPastDueBorrowsAsync(string userId);
        Task<IEnumerable<GetBorrowDto>> GetActiveBorrowsAsync(string userId);
        Task<IEnumerable<GetBorrowDto>> GetRecentlyReturnedBorrowsAsync(string userId, int daysBack = 7);
    }
}
