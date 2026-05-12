using CourtBook.Data;
using CourtBook.Models;
using CourtBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminDashboardController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string date = null,
            string status = null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            var weekStart = today.AddDays(
                -(int)DateTime.Today.DayOfWeek);

            var totalUsers = await _userManager.Users
                .CountAsync();

            var query = _context.Reservations
                .Include(r => r.User)
                .Include(r => r.Court)
                .AsQueryable();

            if (!string.IsNullOrEmpty(date) &&
                DateOnly.TryParse(date, out var parsedDate))
                query = query.Where(r => r.Date == parsedDate);

            if (!string.IsNullOrEmpty(status) &&
                Enum.TryParse<ReservationStatus>(
                    status, out var reservationStatus))
                query = query.Where(r =>
                    r.Status == reservationStatus);

            var recentReservations = await query
                .OrderByDescending(r => r.CreatedAt)
                .Take(10)
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

            var vm = new AdminDashboardViewModel
            {
                TodaysReservationCount = await _context
                    .Reservations
                    .CountAsync(r => r.Date == today),
                TotalActiveCourts = await _context.Courts
                    .CountAsync(c => c.IsActive),
                TotalRegisteredUsers = totalUsers,
                ReservationsThisWeek = await _context
                    .Reservations
                    .CountAsync(r => r.Date >= weekStart),
                // compute total revenue from paid reservations (only confirmed/paid)
                TotalRevenue = await _context.Reservations
                    .Where(r => r.PaymentStatus == PaymentStatus.Paid)
                    .SumAsync(r => (decimal?)r.TotalAmount) ?? 0m,
                RecentReservations = recentReservations,
                FilterDate = date,
                FilterStatus = status
            };

            return View(vm);
        }
    }
}