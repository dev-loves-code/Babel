using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using api.Dtos.Account;
using api.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using api.Models;
using Hangfire;

namespace api.Services
{
    public class AccountService : IAccountService
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly ITokenService _tokenService;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ILogger<AccountService> _logger;


        public AccountService(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, ITokenService tokenService, ILogger<AccountService> logger)
        {
            _tokenService = tokenService;
            _signInManager = signInManager;
            _logger = logger;
            _userManager = userManager;
        }



        public async Task<UserDto> LoginAsync(LoginDto loginDto)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.UserName == loginDto.Username.ToLower());
            if (user == null)
            {
                throw new Exception("User not found.");
            }
            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
            if (result.Succeeded)
            {
                var roles = await _userManager.GetRolesAsync(user);

                return (new UserDto
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    Token = await _tokenService.CreateToken(user),
                    IsBlocked = user.IsBlocked,
                    Role = roles.FirstOrDefault()
                });
            }
            else
            {
                throw new Exception("Invalid password.");
            }
        }



        public async Task<UserDto> RegisterAsync(RegisterDto registerDto)
        {
            var user = new AppUser
            {
                UserName = registerDto.Username,
                Email = registerDto.Email,
                IsBlocked = false
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserName} registered successfully", registerDto.Username);
                BackgroundJob.Enqueue<IEmailService>(x => x.SendWelcomeEmailAsync(registerDto.Email, registerDto.Username));

                var roleResult = await _userManager.AddToRoleAsync(user, "User");
                if (roleResult.Succeeded)
                {
                    _logger.LogInformation("Role 'User' assigned to {UserName}", registerDto.Username);
                    return new UserDto
                    {
                        UserName = user.UserName,
                        Email = user.Email,
                        Token = await _tokenService.CreateToken(user),
                        IsBlocked = user.IsBlocked,
                        Role = "User"
                    };
                }
                else
                {
                    _logger.LogError("Failed to assign role 'User' to {UserName}: {Errors}", registerDto.Username, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    throw new Exception("Failed to assign role to user.");
                }
            }
            else
            {
                _logger.LogError("User registration failed for {UserName}: {Errors}", registerDto.Username, string.Join(", ", result.Errors.Select(e => e.Description)));
                throw new Exception("User registration failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }


        public async Task<bool> BlockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsBlocked = true;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> UnblockUserAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.IsBlocked = false;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<bool> ChangePasswordAsync(string userId, ChangePasswordDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
            return result.Succeeded;
        }

        public async Task<bool> ChangeEmailAsync(string userId, ChangeEmailDto dto)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            user.Email = dto.NewEmail;
            var result = await _userManager.UpdateAsync(user);
            return result.Succeeded;
        }

        public async Task<List<AdminUserDto>> SearchUsersAsync(string query)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
            {
                query = query.ToLower();
                usersQuery = usersQuery.Where(u => u.UserName.ToLower().Contains(query) || u.Email.ToLower().Contains(query));
            }

            var users = await usersQuery
                .Select(u => new AdminUserDto
                {
                    UserId = u.Id,          // ✅ include Id
                    UserName = u.UserName,
                    Email = u.Email,
                    IsBlocked = u.IsBlocked
                })
                .ToListAsync();

            return users;
        }



    }
}