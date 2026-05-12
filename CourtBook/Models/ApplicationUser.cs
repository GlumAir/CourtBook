using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CourtBook.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Reservation> Reservations { get; set; }
    }
}