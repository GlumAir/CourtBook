using CourtBook.Data;
using CourtBook.Models;
using CourtBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CourtBook.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminReservationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminReservationsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
             string dateFrom = null,
             string dateTo = null,
             string sport = null,
             string status = null,
             string payment = null)
        {
            var query = _context.Reservations.AsQueryable();

            // 1. Date range filtering
            if (!string.IsNullOrWhiteSpace(dateFrom) &&
                DateOnly.TryParse(dateFrom, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedFrom))
            {
                query = query.Where(r => r.Date >= parsedFrom);
            }

            if (!string.IsNullOrWhiteSpace(dateTo) &&
                DateOnly.TryParse(dateTo, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedTo))
            {
                query = query.Where(r => r.Date <= parsedTo);
            }

            // 2. Sport filter
            if (!string.IsNullOrWhiteSpace(sport) &&
                Enum.TryParse<SportType>(sport, ignoreCase: true, out var sportType))
            {
                query = query.Where(r => r.Court.SportType == sportType);
            }

            // 3. Status filter
            if (!string.IsNullOrWhiteSpace(status) &&
                Enum.TryParse<ReservationStatus>(status, ignoreCase: true, out var reservationStatus))
            {
                query = query.Where(r => r.Status == reservationStatus);
            }

            // 4. Payment filter
            if (!string.IsNullOrWhiteSpace(payment) &&
                Enum.TryParse<PaymentStatus>(payment, ignoreCase: true, out var paymentStatus))
            {
                query = query.Where(r => r.PaymentStatus == paymentStatus);
            }

            // 5. Project & execute
            var rows = await query
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new AdminReservationRowViewModel
                {
                    Id = r.Id,
                    UserName = r.User.FullName,
                    CourtName = r.Court.Name,
                    SportType = r.Court.SportType,
                    Date = r.Date,
                    StartTime = r.StartTime,
                    EndTime = r.EndTime,
                    TotalAmount = r.TotalAmount,
                    PaymentStatus = r.PaymentStatus,
                    Status = r.Status
                })
                .ToListAsync();

            // Preserve filter values in the view
            ViewBag.FilterDateFrom = dateFrom;
            ViewBag.FilterDateTo = dateTo;
            ViewBag.FilterSport = sport;
            ViewBag.FilterStatus = status;
            ViewBag.FilterPayment = payment;

            return View(rows);
        }



        [HttpPost]
        public async Task<IActionResult> MarkPaid(int id)
        {
            var reservation = await _context.Reservations
                .FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.PaymentStatus = PaymentStatus.Paid;
            reservation.Status = ReservationStatus.Confirmed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation marked as paid.";
            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            var reservation = await _context.Reservations
                .FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.Status = ReservationStatus.Cancelled;
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Reservation cancelled successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> MarkCompleted(int id)
        {
            var reservation = await _context.Reservations
                .FindAsync(id);

            if (reservation == null) return NotFound();

            reservation.Status = ReservationStatus.Completed;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Reservation marked as completed.";
            return RedirectToAction("Index");
        }
    }
}