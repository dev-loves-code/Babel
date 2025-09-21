using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Data;
using api.Dtos.Borrow;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using api.Data;
using api.Dtos.ReturnRequest;

namespace api.Repositories
{
    public class ReturnRequestRepository : IReturnRequestRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<ReturnRequestRepository> _logger;

        public ReturnRequestRepository(ApplicationDBContext context, ILogger<ReturnRequestRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<ReturnRequest>> GetAllReturnRequestsAsync(ReturnRequestQueryObject queryObject)
        {
            var query = _context.ReturnRequests
                .Include(rr => rr.Borrow)
                    .ThenInclude(b => b!.Book)
                .Include(rr => rr.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(queryObject.UserId))
                query = query.Where(rr => rr.UserId == queryObject.UserId);

            if (!string.IsNullOrEmpty(queryObject.UserName))
                query = query.Where(rr => rr.User.UserName!.Contains(queryObject.UserName));

            if (queryObject.Status.HasValue)
                query = query.Where(rr => rr.Status == queryObject.Status.Value);

            if (queryObject.RequestDateFrom.HasValue)
                query = query.Where(rr => rr.RequestDate >= queryObject.RequestDateFrom.Value);

            if (queryObject.RequestDateTo.HasValue)
                query = query.Where(rr => rr.RequestDate <= queryObject.RequestDateTo.Value);

            return await query.OrderByDescending(rr => rr.RequestDate).ToListAsync();
        }

        public async Task<ReturnRequest?> GetReturnRequestByIdAsync(int id)
        {
            return await _context.ReturnRequests
                .Include(rr => rr.Borrow)
                    .ThenInclude(b => b!.Book)
                .Include(rr => rr.User)
                .FirstOrDefaultAsync(rr => rr.Id == id);
        }

        public async Task<ReturnRequest?> GetReturnRequestByBorrowIdAsync(int borrowId)
        {
            return await _context.ReturnRequests
                .Include(rr => rr.Borrow)
                    .ThenInclude(b => b!.Book)
                .Include(rr => rr.User)
                .FirstOrDefaultAsync(rr => rr.BorrowId == borrowId);
        }

        public async Task<ReturnRequest> CreateReturnRequestAsync(ReturnRequest returnRequest)
        {
            _context.ReturnRequests.Add(returnRequest);
            await _context.SaveChangesAsync();
            return returnRequest;
        }

        public async Task<ReturnRequest> UpdateReturnRequestAsync(ReturnRequest returnRequest)
        {
            _context.ReturnRequests.Update(returnRequest);
            await _context.SaveChangesAsync();
            return returnRequest;
        }

        public async Task<bool> ReturnRequestExistsAsync(int id)
        {
            return await _context.ReturnRequests.AnyAsync(rr => rr.Id == id);
        }

        public async Task<bool> HasPendingReturnRequestAsync(int borrowId)
        {
            return await _context.ReturnRequests
                .AnyAsync(rr => rr.BorrowId == borrowId && rr.Status == ReturnStatus.Pending);
        }
    }
}