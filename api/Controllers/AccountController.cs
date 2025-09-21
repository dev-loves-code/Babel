using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using api.Dtos.Account;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace api.Controllers
{
    [Route("api/account")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<AccountController> _logger;
        private readonly INotificationService _notifications;

        public AccountController(IAccountService accountService, ILogger<AccountController> logger, INotificationService notifications)
        {
            _logger = logger;
            _accountService = accountService;
            _notifications = notifications;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            if (registerDto == null)
            {
                return BadRequest("Invalid registration data.");
            }

            try
            {
                var user = await _accountService.RegisterAsync(registerDto);
                if (user == null)
                {
                    return BadRequest("Registration failed.");
                }

                _logger.LogInformation("User {UserName} registered successfully", registerDto.Username);

                return Ok(user);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user {UserName}", registerDto.Username);
                return StatusCode(500, ex.Message);
            }


        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto == null)
            {
                return BadRequest("Invalid login data.");
            }

            try
            {
                var user = await _accountService.LoginAsync(loginDto);
                if (user == null)
                {
                    return Unauthorized("Login failed.");
                }
                _logger.LogInformation("User {UserName} logged in successfully", loginDto.Username);

                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user {UserName}", loginDto.Username);
                return BadRequest(new { message = "Invalid password." });
            }


        }



        [Authorize(Roles = "Admin")]
        [HttpPost("block/{userId}")]
        public async Task<IActionResult> BlockUser(string userId)
        {
            try
            {
                var result = await _accountService.BlockUserAsync(userId);
                if (!result)
                    return BadRequest("Failed to block user.");

                _logger.LogInformation("User {UserId} blocked successfully", userId);
                return Ok("User blocked successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to block user {UserId}", userId);
                return StatusCode(500, ex.Message);
            }
        }


        [Authorize(Roles = "Admin")]
        [HttpPost("unblock/{userId}")]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            try
            {
                var result = await _accountService.UnblockUserAsync(userId);
                if (!result)
                    return BadRequest("Failed to unblock user.");

                _logger.LogInformation("User {UserId} unblocked successfully", userId);
                return Ok("User unblocked successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unblock user {UserId}", userId);
                return StatusCode(500, ex.Message);
            }
        }


        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token.");
                }

                var result = await _accountService.ChangePasswordAsync(userId, dto);
                if (!result)
                    return BadRequest("Failed to change password. Check your current password.");

                _logger.LogInformation("Password changed successfully for User {UserId}", userId);
                await _notifications.SendPrivateMessageAsync(userId, "Password updated successfully.");
                return Ok("Password changed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password for User {UserId}", User?.Identity?.Name);
                return StatusCode(500, ex.Message);
            }
        }



        [Authorize]
        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeEmail([FromBody] ChangeEmailDto dto)
        {
            try
            {
                var userId = User.Claims.First(c => c.Type == "id").Value;
                var result = await _accountService.ChangeEmailAsync(userId, dto);
                if (!result)
                    return BadRequest("Failed to update email.");

                _logger.LogInformation("Email updated successfully for User {UserId}", userId);
                await _notifications.SendPrivateMessageAsync(userId, "Email updated successfully.");
                return Ok("Email updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update email for User {UserId}", User?.Identity?.Name);
                return StatusCode(500, ex.Message);
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string query = null)
        {
            try
            {
                var users = await _accountService.SearchUsersAsync(query);
                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query: {Query}", query);
                return StatusCode(500, "An error occurred while searching users.");
            }
        }


    }
}