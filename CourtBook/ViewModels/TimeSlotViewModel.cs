namespace CourtBook.ViewModels
{
    public class TimeSlotViewModel
    {
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public bool IsAvailable { get; set; }
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
