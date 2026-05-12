using CourtBook.ViewModels;
using CourtBook.Models;

namespace CourtBook.Services
{
    public class TimeSlotService
    {
        public List<TimeSlotViewModel> GenerateSlots(
            string operatingHours,
            DateOnly date,
            List<Reservation> existingReservations)
        {
            var slots = new List<TimeSlotViewModel>();

            var parts = operatingHours
                .Split('-', StringSplitOptions.TrimEntries);

            if (parts.Length != 2) return slots;

            if (!TryParseTime(parts[0], out var start) ||
                !TryParseTime(parts[1], out var end))
                return slots;

            var current = start;
            while (current < end)
            {
                var slotEnd = current.Add(TimeSpan.FromHours(1));
                var startStr = current.ToString(@"hh\:mm");

                var isTaken = existingReservations.Any(r =>
                    r.Date == date &&
                    r.Status == ReservationStatus.Confirmed &&
                    r.StartTime == startStr);

                slots.Add(new TimeSlotViewModel
                {
                    StartTime = startStr,
                    EndTime = slotEnd.ToString(@"hh\:mm"),
                    IsAvailable = !isTaken
                });

                current = slotEnd;
            }

            return slots;
        }

        private bool TryParseTime(string input, out TimeSpan result)
        {
            if (DateTime.TryParse(input, out var dt))
            {
                result = dt.TimeOfDay;
                return true;
            }
            result = default;
            return false;
        }
    }

}
