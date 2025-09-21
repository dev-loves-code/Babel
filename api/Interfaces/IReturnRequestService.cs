using System.Collections.Generic;
using System.Threading.Tasks;
using api.Dtos.Borrow;
using api.Dtos.ReturnRequest;

namespace api.Interfaces
{

    public interface IReturnRequestService
    {
        Task<IEnumerable<GetReturnRequestDto>> GetAllReturnRequestsAsync(ReturnRequestQueryObject queryObject);
        Task<GetReturnRequestDto?> GetReturnRequestByIdAsync(int id);
        Task<IEnumerable<GetReturnRequestDto>> GetUserReturnRequestsAsync(string userId);
        Task<GetReturnRequestDto> CreateReturnRequestAsync(string userId, CreateReturnRequestDto requestDto);
        Task<GetReturnRequestDto> ProcessReturnRequestAsync(int requestId, ProcessReturnRequestDto processDto);
        Task<GetBorrowDto> DirectReturnAsync(DirectReturnDto directReturnDto);
        Task<IEnumerable<GetReturnRequestDto>> GetPendingReturnRequestsAsync(string userId);
        Task<IEnumerable<GetReturnRequestDto>> GetRecentReturnRequestsAsync(string userId, int daysBack = 7);
    }
}
