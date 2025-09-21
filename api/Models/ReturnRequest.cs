using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class ReturnRequest
    {
        public int Id { get; set; }

        public int BorrowId { get; set; }
        public Borrow? Borrow { get; set; }


        public string UserId { get; set; }
        public AppUser User { get; set; }

        public DateTime RequestDate { get; set; }

        public ReturnStatus Status { get; set; }
    }

    public enum ReturnStatus
    {
        Pending,
        Accepted
    }
}