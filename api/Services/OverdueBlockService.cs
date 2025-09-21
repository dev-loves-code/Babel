using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Services
{
    public class OverdueBlockService : IOverdueBlockService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IBorrowService _borrowService; // where GetPastDueBorrowsAsync lives
        private readonly ILogger<OverdueBlockService> _logger;

        public OverdueBlockService(
            UserManager<AppUser> userManager,
            IBorrowService borrowService,
            ILogger<OverdueBlockService> logger)
        {
            _userManager = userManager;
            _borrowService = borrowService;
            _logger = logger;
        }

        public async Task BlockUsersWithPastDueBorrowsAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var pastDueBorrows = await _borrowService.GetPastDueBorrowsAsync(user.Id);

                if (pastDueBorrows.Count() >= 2 && !user.IsBlocked)
                {
                    user.IsBlocked = true;
                    var result = await _userManager.UpdateAsync(user);

                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Blocked user {UserId} due to {Count} overdue borrows",
                            user.Id, pastDueBorrows.Count());
                    }
                    else
                    {
                        _logger.LogWarning("Failed to block user {UserId}", user.Id);
                    }
                }
            }
        }
    }

}