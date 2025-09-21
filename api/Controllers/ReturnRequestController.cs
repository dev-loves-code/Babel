using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using api.Dtos.Borrow;
using api.Dtos.ReturnRequest;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReturnRequestController : ControllerBase
    {
        private readonly IReturnRequestService _returnRequestService;
        private readonly ILogger<ReturnRequestController> _logger;
        private readonly INotificationService _notifications;

        public ReturnRequestController(IReturnRequestService returnRequestService, ILogger<ReturnRequestController> logger, INotificationService notifications)
        {
            _returnRequestService = returnRequestService;
            _logger = logger;
            _notifications = notifications;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<GetReturnRequestDto>>> GetAllReturnRequests([FromQuery] ReturnRequestQueryObject queryObject)
        {
            try
            {
                var returnRequests = await _returnRequestService.GetAllReturnRequestsAsync(queryObject);
                return Ok(returnRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all return requests");
                return StatusCode(500, "Internal server error occurred while retrieving return requests");
            }
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<GetReturnRequestDto>> GetReturnRequestById(int id)
        {
            try
            {
                var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var returnRequest = await _returnRequestService.GetReturnRequestByIdAsync(id);

                if (returnRequest == null)
                    return NotFound($"Return request with ID {id} not found");

                // Users can only view their own return requests, admins can view all
                if (!User.IsInRole("Admin") && returnRequest.UserId != currentUserId)
                    return Forbid();

                return Ok(returnRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving return request {RequestId}", id);
                return StatusCode(500, "Internal server error occurred while retrieving return request");
            }
        }

        [HttpGet("my-requests")]
        public async Task<ActionResult<IEnumerable<GetReturnRequestDto>>> GetMyReturnRequests()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var returnRequests = await _returnRequestService.GetUserReturnRequestsAsync(userId);
                return Ok(returnRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user return requests");
                return StatusCode(500, "Internal server error occurred while retrieving your return requests");
            }
        }

        [HttpPost]
        public async Task<ActionResult<GetReturnRequestDto>> CreateReturnRequest([FromBody] CreateReturnRequestDto requestDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized();

                var returnRequest = await _returnRequestService.CreateReturnRequestAsync(userId, requestDto);

                await _notifications.SendPrivateMessageAsync(userId, $"You have sent a return request for the borrow with ID {requestDto.BorrowId}.");
                return CreatedAtAction(nameof(GetReturnRequestById), new { id = returnRequest.Id }, returnRequest);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Resource not found when creating return request");
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized access when creating return request");
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when creating return request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating return request");
                return StatusCode(500, "Internal server error occurred while creating return request");
            }
        }

        [HttpPut("{id:int}/process")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<GetReturnRequestDto>> ProcessReturnRequest(int id, [FromBody] ProcessReturnRequestDto processDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var returnRequest = await _returnRequestService.ProcessReturnRequestAsync(id, processDto);
                return Ok(returnRequest);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Return request not found when processing");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation when processing return request");
                return BadRequest(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid argument when processing return request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing return request {RequestId}", id);
                return StatusCode(500, "Internal server error occurred while processing return request");
            }
        }

        [HttpPost("direct-return")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<GetBorrowDto>> DirectReturn([FromBody] DirectReturnDto directReturnDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var borrow = await _returnRequestService.DirectReturnAsync(directReturnDto);
                return Ok(borrow);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Borrow not found for direct return");
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid operation for direct return");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing direct return");
                return StatusCode(500, "Internal server error occurred while processing direct return");
            }
        }
    }
}