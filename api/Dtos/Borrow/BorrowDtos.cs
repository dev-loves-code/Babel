using System;
using System.ComponentModel.DataAnnotations;
using api.Models;


namespace api.Dtos.Borrow
{
    public class CreateBorrowDto
    {
        [Required]
        public int BookId { get; set; }
    }

    public class GetBorrowDto
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string BookTitle { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public BorrowStatus Status { get; set; }
        public bool HasPendingReturnRequest { get; set; }
    }

    public class BorrowQueryObject
    {
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? BookTitle { get; set; }
        public BorrowStatus? Status { get; set; }
        public bool? IncludeReturned { get; set; } = false;
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
    }

    public enum BorrowStatus
    {
        Active,
        Returned,
        ReturnedPastDue,
        PastDue
    }
}