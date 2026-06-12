using System.ComponentModel.DataAnnotations;
using CourtBook.Models;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace CourtBook.ViewModels
{
    public class BookCourtViewModel
    {
        public int CourtId { get; set; }
        // display-only values - ignore during model binding so they don't produce validation errors
        [BindNever]
        public string? CourtName { get; set; }
        public SportType SportType { get; set; }
        [BindNever]
        public string? OperatingHours { get; set; }
        public decimal PricePerHour { get; set; }

        [Required]
        public DateOnly? SelectedDate { get; set; }

        public string? SelectedStartTime { get; set; }

        public string? SelectedEndTime { get; set; }

        public List<TimeSlotViewModel> TimeSlots { get; set; }
            = new();
    }

}
