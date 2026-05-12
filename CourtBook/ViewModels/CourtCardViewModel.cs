using CourtBook.Models;


namespace CourtBook.ViewModels
{
    public class CourtCardViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SportType SportType { get; set; }
        public string OperatingHours { get; set; }
        public decimal PricePerHour { get; set; }
        public bool IsActive { get; set; }
    }
}