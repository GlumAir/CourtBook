using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CourtBook.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public int CourtId { get; set; }

        [Required]
        public DateOnly Date { get; set; }

        [Required]
        [MaxLength(5)]
        public string StartTime { get; set; }

        [Required]
        [MaxLength(5)]
        public string EndTime { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        public PaymentStatus PaymentStatus { get; set; }
            = PaymentStatus.Unpaid;

        public ReservationStatus Status { get; set; }
            = ReservationStatus.Reserved;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; }
        public Court Court { get; set; }
    }

}
