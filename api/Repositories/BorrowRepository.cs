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
    public class BorrowRepository : IBorrowRepository
    {
        private readonly ApplicationDBContext _context;
        private readonly ILogger<BorrowRepository> _logger;

        public BorrowRepository(ApplicationDBContext context, ILogger<BorrowRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Borrow>> GetAllBorrowsAsync(BorrowQueryObject queryObject)
        {
            var query = _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.User)
                .Include(b => b.ReturnRequest)
                .AsQueryable();

            if (!string.IsNullOrEmpty(queryObject.UserId))
                query = query.Where(b => b.UserId == queryObject.UserId);

            if (!string.IsNullOrEmpty(queryObject.UserName))
                query = query.Where(b => b.User.UserName!.Contains(queryObject.UserName));

            if (!string.IsNullOrEmpty(queryObject.BookTitle))
                query = query.Where(b => b.Book!.Title.Contains(queryObject.BookTitle));

            if (queryObject.StartDateFrom.HasValue)
                query = query.Where(b => b.StartDate >= queryObject.StartDateFrom.Value);

            if (queryObject.StartDateTo.HasValue)
                query = query.Where(b => b.StartDate <= queryObject.StartDateTo.Value);

            if (queryObject.Status == BorrowStatus.PastDue)
            {
                var now = DateTime.UtcNow;
                query = query.Where(b => b.ReturnDate == null &&
                            b.StartDate.AddDays(7) < now);
            }

            if (queryObject.IncludeReturned == false)
            {
                query = query.Where(b => b.ReturnDate == null);
            }


            return await query.OrderByDescending(b => b.StartDate).ToListAsync();
        }

        public async Task<Borrow?> GetBorrowByIdAsync(int id)
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.User)
                .Include(b => b.ReturnRequest)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Borrow?> GetBorrowByBookAndUserAsync(int bookId, string userId)
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.User)
                .Include(b => b.ReturnRequest)
                .Where(b => b.UserId == userId && b.ReturnDate == null)
                .FirstOrDefaultAsync(b => b.BookId == bookId && b.UserId == userId);
        }


        public async Task<IEnumerable<Borrow>> GetActiveBorrowsByUserAsync(string userId)
        {
            return await _context.Borrows
                .Include(b => b.Book)
                .Include(b => b.ReturnRequest)
                .Where(b => b.UserId == userId && b.ReturnDate == null)
                .ToListAsync();
        }

        public async Task<int> GetActiveBorrowCountByUserAsync(string userId)
        {
            return await _context.Borrows
                .CountAsync(b => b.UserId == userId && b.ReturnDate == null);
        }

        public async Task<int> GetActiveAndPastDueBorrowCountByBookAsync(int bookId)
        {
            return await _context.Borrows
                .CountAsync(b => b.BookId == bookId && b.ReturnDate == null);
        }

        public async Task<Borrow> CreateBorrowAsync(Borrow borrow)
        {
            _context.Borrows.Add(borrow);
            await _context.SaveChangesAsync();
            return borrow;
        }

        public async Task<Borrow> UpdateBorrowAsync(Borrow borrow)
        {
            _context.Borrows.Update(borrow);
            await _context.SaveChangesAsync();
            return borrow;
        }

        public async Task<bool> BorrowExistsAsync(int id)
        {
            return await _context.Borrows.AnyAsync(b => b.Id == id);
        }

        public async Task<bool> HasActiveBorrowForBookAsync(string userId, int bookId)
        {
            return await _context.Borrows
                .AnyAsync(b => b.UserId == userId && b.BookId == bookId && b.ReturnDate == null);
        }
    }

}