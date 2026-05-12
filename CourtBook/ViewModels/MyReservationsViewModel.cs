namespace CourtBook.ViewModels
{
    public class MyReservationsViewModel
    {
        public List<ReservationCardViewModel> Upcoming { get; set; }
            = new();
        public List<ReservationCardViewModel> Past { get; set; }
            = new();
        public string ActiveTab { get; set; } = "upcoming";
    }

}
