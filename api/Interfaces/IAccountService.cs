using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;

namespace api.Interfaces
{
    public interface IAccountService
    {
        Task<UserDto> RegisterAsync(RegisterDto registerDto);
        Task<UserDto> LoginAsync(LoginDto loginDto);
        Task<bool> BlockUserAsync(string userId);
        Task<bool> UnblockUserAsync(string userId);
        Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto);
        Task<bool> ChangeEmailAsync(string userId, ChangeEmailDto dto);
        Task<List<AdminUserDto>> SearchUsersAsync(string query);
    }
}