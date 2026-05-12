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
                    r.Status == ReservationStatus.Confirmed)
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
        public async Task<IActionResult> Book(BookCourtViewModel model)
        {
            // try to bind SelectedDate from the raw form if model binder didn't set it (DateOnly may not bind)
            if (model.SelectedDate == null)
            {
                if (Request.Form.TryGetValue("SelectedDate", out var sd) && !string.IsNullOrEmpty(sd))
                {
                    if (DateOnly.TryParse(sd, out var parsed))
                    {
                        model.SelectedDate = parsed;
                        // clear any modelstate errors for SelectedDate created by the default binder
                        ModelState.Remove(nameof(model.SelectedDate));
                    }
                }
            }

            // Basic validation: ensure date and times exist
            if (model.SelectedDate == null)
                ModelState.AddModelError("SelectedDate", "Date is required.");

            // Trim posted times to avoid whitespace mismatches
            model.SelectedStartTime = model.SelectedStartTime?.Trim();
            model.SelectedEndTime = model.SelectedEndTime?.Trim();

            if (string.IsNullOrEmpty(model.SelectedStartTime) || string.IsNullOrEmpty(model.SelectedEndTime))
                ModelState.AddModelError("", "Please select a time slot.");

            if (!ModelState.IsValid)
            {
                _logger?.LogWarning("Invalid model state when booking reservation. Errors: {Errors}. Posted values: CourtId={CourtId}, SelectedDate={SelectedDate}, Start={Start}, End={End}",
                    string.Join(";", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)),
                    model?.CourtId, model?.SelectedDate, model?.SelectedStartTime, model?.SelectedEndTime);
                await PopulateBookModel(model);
                return View(model);
            }

            // ✅ Parse the strings first
            if (!TimeSpan.TryParse(model.SelectedStartTime, out var startTime) ||
                !TimeSpan.TryParse(model.SelectedEndTime, out var endTime))
            {
                ModelState.AddModelError("", "Invalid time format.");
                await PopulateBookModel(model);
                return View(model);
            }

            var court = await _context.Courts.FindAsync(model.CourtId);
            if (court == null)
                return NotFound();

            // Convert TimeSpan to the same string format used in Reservation ("hh:mm")
            var startTimeStr = startTime.ToString(@"hh\:mm");
            var endTimeStr = endTime.ToString(@"hh\:mm");

            // ✅ Use the formatted string for comparisons against the stored string
            var conflict = await _context.Reservations
                .AnyAsync(r =>
                    r.CourtId == model.CourtId &&
                    r.Date == model.SelectedDate &&
                    r.StartTime == startTimeStr &&
                    r.EndTime == endTimeStr &&
                    r.Status == ReservationStatus.Confirmed);

            if (conflict)
            {
                ModelState.AddModelError("", "Selected time slot is no longer available.");
                await PopulateBookModel(model);
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account", new { returnUrl = Url.Action("Book", "Reservations", new { courtId = model.CourtId }) });
            }

            // ✅ Store times as strings to match the Reservation model
            var reservation = new Reservation
            {
                UserId = userId,
                CourtId = model.CourtId,
                Date = model.SelectedDate.Value,
                StartTime = startTimeStr,
                EndTime = endTimeStr,
                TotalAmount = court.PricePerHour,
                PaymentStatus = PaymentStatus.Unpaid,
                Status = ReservationStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };

            // Use a transaction and re-check conflicts immediately before saving to avoid races
            using (var tx = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // re-check conflict right before insert
                    var stillConflict = await _context.Reservations
                        .AnyAsync(r =>
                            r.CourtId == model.CourtId &&
                            r.Date == model.SelectedDate &&
                            r.StartTime == startTimeStr &&
                            r.EndTime == endTimeStr &&
                            r.Status == ReservationStatus.Confirmed);

                    if (stillConflict)
                    {
                        // someone reserved while the user was confirming
                        ModelState.AddModelError("", "Selected time slot is no longer available.");
                        await PopulateBookModel(model);
                        return View(model);
                    }

                    _context.Reservations.Add(reservation);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (DbUpdateException ex)
                {
                    _logger?.LogError(ex, "DbUpdateException saving reservation for user {UserId}", userId);
                    // possible unique constraint race or DB issue
                    ModelState.AddModelError("", "Unable to save reservation. Please try again.");
                    await PopulateBookModel(model);
                    return View(model);
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Unexpected error saving reservation for user {UserId}", userId);
                    ModelState.AddModelError("", "An unexpected error occurred.");
                    await PopulateBookModel(model);
                    return View(model);
                }
            }

            TempData["Success"] = "Reservation confirmed successfully.";
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
                    r.Status == ReservationStatus.Confirmed)
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
                .Where(r => r.CourtId == model.CourtId && r.Date == selectedDate && r.Status == ReservationStatus.Confirmed)
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