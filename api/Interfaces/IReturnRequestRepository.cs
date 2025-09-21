using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.ReturnRequest;
using api.Models;

namespace api.Interfaces
{

    public interface IReturnRequestRepository
    {
        Task<IEnumerable<ReturnRequest>> GetAllReturnRequestsAsync(ReturnRequestQueryObject queryObject);
        Task<ReturnRequest?> GetReturnRequestByIdAsync(int id);
        Task<ReturnRequest?> GetReturnRequestByBorrowIdAsync(int borrowId);
        Task<ReturnRequest> CreateReturnRequestAsync(ReturnRequest returnRequest);
        Task<ReturnRequest> UpdateReturnRequestAsync(ReturnRequest returnRequest);
        Task<bool> ReturnRequestExistsAsync(int id);
        Task<bool> HasPendingReturnRequestAsync(int borrowId);
    }
}