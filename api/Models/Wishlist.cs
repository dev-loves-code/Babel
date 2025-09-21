using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace api.Models
{
    public class Wishlist
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int BookId { get; set; }

        public AppUser User { get; set; }
        public Book Book { get; set; }
    }

}