using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Borrow
    {
        public int Id { get; set; }

        public int BookId { get; set; }
        public Book? Book { get; set; }

        // From Identity
        public string UserId { get; set; }
        public AppUser User { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? ReturnDate { get; set; }

        public ReturnRequest? ReturnRequest { get; set; }
    }
}