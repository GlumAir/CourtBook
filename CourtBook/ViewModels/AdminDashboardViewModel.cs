namespace CourtBook.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TodaysReservationCount { get; set; }
        public int TotalActiveCourts { get; set; }
        public int TotalRegisteredUsers { get; set; }
        public int ReservationsThisWeek { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<AdminReservationRowViewModel>
            RecentReservations
        { get; set; } = new();

        public string FilterDate { get; set; }
        public string FilterStatus { get; set; }
    }

}
