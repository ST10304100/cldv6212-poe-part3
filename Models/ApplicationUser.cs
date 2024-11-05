using Microsoft.AspNetCore.Identity;

namespace CLDV6212_PART_3.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Firstname { get; set; }
        public string Lastname { get; set; }
    }
}
