using CourtBook.Data;
using CourtBook.Models;
using CourtBook.Services;
using CourtBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CourtBook.Controllers
{
    [Authorize(Roles = "User")]
    public class ReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly TimeSlotService _timeSlotService;
        private readonly ILogger<ReservationsController> _logger;

        public ReservationsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            TimeSlotService timeSlotService,
            ILogger<ReservationsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _timeSlotService = timeSlotService;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Book(
            int courtId,
            string date = null)
        {
            var court = await _context.Courts
                .FirstOrDefaultAsync(c =>
                    c.Id == courtId && c.IsActive);

            if (court == null)
                return NotFound();

            DateOnly selectedDate = date != null
                ? DateOnly.Parse(date)
                : DateOnly.FromDateTime(DateTime.Today);

            var existing = await _context.Reservations
                .Where(r =>
                    r.CourtId == courtId &&
                    r.Date == selectedDate &&
                    (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Reserved))
                .ToListAsync();

            var slots = _timeSlotService.GenerateSlots(
                court.OperatingHours,
                selectedDate,
                existing);

            var vm = new BookCourtViewModel
            {
                CourtId = court.Id,
                CourtName = court.Name,
                SportType = court.SportType,
                OperatingHours = court.OperatingHours,
                PricePerHour = court.PricePerHour,
                SelectedDate = selectedDate,
                TimeSlots = slots
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Book(BookCourtViewModel model, List<string> selectedSlots)
        {
            // Try to bind SelectedDate from the raw form if model binder didn't set it
            if (model.SelectedDate == null)
            {
                if (Request.Form.TryGetValue("SelectedDate", out var sd) && !string.IsNullOrEmpty(sd))
                {
                    if (DateOnly.TryParse(sd, out var parsed))
                    {
                        model.SelectedDate = parsed;
                        ModelState.Remove(nameof(model.SelectedDate));
                    }
                }
            }

            // Basic validation: ensure date exists and at least one slot is selected
            if (model.SelectedDate == null)
                ModelState.AddModelError("SelectedDate", "Date is required.");

            if (selectedSlots == null || !selectedSlots.Any())
                ModelState.AddModelError("", "Please select at least one time slot.");

            if (!ModelState.IsValid)
            {
                _logger?.LogWarning("Invalid model state when booking multi-reservation. Posted values: CourtId={CourtId}, SelectedDate={SelectedDate}",
                    model?.CourtId, model?.SelectedDate);
                await PopulateBookModel(model);
                return View(model);
            }

            var court = await _context.Courts.FindAsync(model.CourtId);
            if (court == null)
                return NotFound();

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Book", "Reservations", new { courtId = model.CourtId }) });
            }

            int successfulBookings = 0;

            // Use a transaction to safely apply batch additions and completely avoid race conditions
            using (var tx = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var slotString in selectedSlots)
                    {
                        // Splits "hh:mm-hh:mm" format cleanly into individual tokens
                        var parts = slotString.Split('-');
                        if (parts.Length != 2) continue;

                        if (!TimeSpan.TryParse(parts[0].Trim(), out var startTime) ||
                            !TimeSpan.TryParse(parts[1].Trim(), out var endTime))
                        {
                            continue;
                        }

                        var startTimeStr = startTime.ToString(@"hh\:mm");
                        var endTimeStr = endTime.ToString(@"hh\:mm");

                        // Double check availability within transaction boundaries
                        var isConflict = await _context.Reservations
                            .AnyAsync(r =>
                                r.CourtId == model.CourtId &&
                                r.Date == model.SelectedDate &&
                                r.StartTime == startTimeStr &&
                                r.EndTime == endTimeStr &&
                                (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Reserved));

                        if (isConflict) continue; // Skip taken blocks smoothly

                        var reservation = new Reservation
                        {
                            UserId = userId,
                            CourtId = model.CourtId,
                            Date = model.SelectedDate.Value,
                            StartTime = startTimeStr,
                            EndTime = endTimeStr,
                            TotalAmount = court.PricePerHour,
                            PaymentStatus = PaymentStatus.Unpaid,
                            Status = ReservationStatus.Reserved,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.Reservations.Add(reservation);
                        successfulBookings++;
                    }

                    if (successfulBookings > 0)
                    {
                        await _context.SaveChangesAsync();
                        await tx.CommitAsync();
                    }
                    else
                    {
                        ModelState.AddModelError("", "All selected slots are no longer available.");
                        await PopulateBookModel(model);
                        return View(model);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Unexpected error saving batch reservations for user {UserId}", userId);
                    ModelState.AddModelError("", "An unexpected error occurred while saving your reservations.");
                    await PopulateBookModel(model);
                    return View(model);
                }
            }

            TempData["Success"] = $"Successfully reserved {successfulBookings} time slot(s).";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string tab = "upcoming")
        {
            var userId = _userManager.GetUserId(User);
            var today = DateOnly.FromDateTime(DateTime.Today);

            var all = await _context.Reservations
                .Include(r => r.Court)
                .Where(r => r.UserId == userId)
                .ToListAsync();

            var upcoming = all
                .Where(r =>
                    r.Date >= today &&
                    (r.Status == ReservationStatus.Confirmed ||
                     r.Status == ReservationStatus.Reserved))
                .OrderBy(r => r.Date)
                .Select(r => MapToCard(r))
                .ToList();

            var past = all
                .Where(r =>
                    r.Date < today ||
                    r.Status == ReservationStatus.Cancelled)
                .OrderByDescending(r => r.Date)
                .Select(r => MapToCard(r))
                .ToList();

            return View(new MyReservationsViewModel
            {
                Upcoming = upcoming,
                Past = past,
                ActiveTab = tab
            });
        }

        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = _userManager.GetUserId(User);

            var reservation = await _context.Reservations
                .FirstOrDefaultAsync(r =>
                    r.Id == id &&
                    r.UserId == userId);

            if (reservation == null)
                return Forbid();

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Reservation cancelled successfully.";

            return RedirectToAction("Index",
                new { tab = "upcoming" });
        }

        private ReservationCardViewModel MapToCard(
            Reservation r) => new()
            {
                Id = r.Id,
                CourtName = r.Court.Name,
                SportType = r.Court.SportType,
                Date = r.Date,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                TotalAmount = r.TotalAmount,
                PaymentStatus = r.PaymentStatus,
                Status = r.Status
            };

        private async Task PopulateBookModel(BookCourtViewModel model)
        {
            if (model == null) return;

            var court = await _context.Courts.FindAsync(model.CourtId);
            if (court == null) return;

            var selectedDate = model.SelectedDate ?? DateOnly.FromDateTime(DateTime.Today);

            var existing = await _context.Reservations
                .Where(r => r.CourtId == model.CourtId && r.Date == selectedDate && (r.Status == ReservationStatus.Confirmed || r.Status == ReservationStatus.Reserved))
                .ToListAsync();

            var slots = _timeSlotService.GenerateSlots(court.OperatingHours, selectedDate, existing);

            model.CourtName = court.Name;
            model.SportType = court.SportType;
            model.OperatingHours = court.OperatingHours;
            model.PricePerHour = court.PricePerHour;
            model.SelectedDate = selectedDate;
            model.TimeSlots = slots;
        }
    }
}