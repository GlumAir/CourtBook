using CourtBook.Models;
using System.ComponentModel.DataAnnotations;

namespace CourtBook.ViewModels
{
    public class EditCourtViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Court name is required.")]
        [MaxLength(100, ErrorMessage =
            "Court name cannot exceed 100 characters.")]
        [Display(Name = "Court Name")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Sport type is required.")]
        [Display(Name = "Sport Type")]
        public SportType SportType { get; set; }

        [Required(ErrorMessage =
            "Operating hours are required.")]
        [MaxLength(50, ErrorMessage =
            "Operating hours cannot exceed 50 characters.")]
        [Display(Name = "Operating Hours")]
        [RegularExpression(
            @"^\d{1,2}:\d{2}\s?(AM|PM)\s?-\s?\d{1,2}:\d{2}\s?(AM|PM)$",
            ErrorMessage =
                "Format must be like: 6:00 AM - 10:00 PM")]
        public string OperatingHours { get; set; }

        [Required(ErrorMessage =
            "Price per hour is required.")]
        [Range(1, 10000, ErrorMessage =
            "Price must be between 1 and 10,000.")]
        [DataType(DataType.Currency)]
        [Display(Name = "Price Per Hour")]
        public decimal PricePerHour { get; set; }
    }
}