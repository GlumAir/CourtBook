using CourtBook.Models;

namespace CourtBook.ViewModels
{
    public class AdminReservationRowViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string CourtName { get; set; }
        public SportType SportType { get; set; }
        public DateOnly Date { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public decimal TotalAmount { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public ReservationStatus Status { get; set; }

        public string DisplayStart =>
            FormatTime(StartTime);
        public string DisplayEnd =>
            FormatTime(EndTime);

        private string FormatTime(string time)
        {
            if (TimeSpan.TryParse(time, out var ts))
            {
                var dt = DateTime.Today.Add(ts);
                return dt.ToString("h:mm tt");
            }
            return time;
        }
    }

}
