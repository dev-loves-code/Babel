using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

namespace api.Models
{
    public class AppUser : IdentityUser
    {
        public bool IsBlocked { get; set; }

        public List<Borrow>? Borrows { get; set; }
        public List<ReturnRequest>? ReturnRequests { get; set; }
        public List<Wishlist>? Wishlists { get; set; }
    }
}