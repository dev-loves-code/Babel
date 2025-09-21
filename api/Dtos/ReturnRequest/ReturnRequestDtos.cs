using System;
using System.ComponentModel.DataAnnotations;
using api.Models;


namespace api.Dtos.ReturnRequest
{
    public class CreateReturnRequestDto
    {
        [Required]
        public int BorrowId { get; set; }
    }

    public class GetReturnRequestDto
    {
        public int Id { get; set; }
        public int BorrowId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public ReturnStatus Status { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }

    public class ProcessReturnRequestDto
    {
        [Required]
        public ReturnStatus Status { get; set; } // Accept or Decline
    }

    public class DirectReturnDto
    {
        [Required]
        public int BorrowId { get; set; }
    }

    public class ReturnRequestQueryObject
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public ReturnStatus? Status { get; set; }
        public DateTime? RequestDateFrom { get; set; }
        public DateTime? RequestDateTo { get; set; }
    }
}