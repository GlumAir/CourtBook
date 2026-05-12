using CourtBook.Data;
using CourtBook.Models;
using CourtBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            // provide simple site-wide stats for the landing page counters
            ViewData["TotalCourts"] = await _context.Courts.CountAsync();
            ViewData["TotalReservations"] = await _context.Reservations.CountAsync();
            ViewData["TotalUsers"] = await _userManager.Users.CountAsync();
            return View();
        }

        [Authorize(Roles = "User")]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            ApplicationUser? user = null;
            if (!string.IsNullOrEmpty(userId))
            {
                user = await _userManager.FindByIdAsync(userId);
            }
            // Prefer the user's full name when available.
            // If UserName is an email, show the part before '@' as a friendly username.
            string? displayName = null;
            if (!string.IsNullOrEmpty(user?.FullName))
                displayName = user.FullName;
            else if (!string.IsNullOrEmpty(user?.UserName))
            {
                var uname = user.UserName;
                if (uname.Contains("@"))
                    displayName = uname.Split('@')[0];
                else
                    displayName = uname;
            }
            ViewData["Username"] = displayName ?? User.Identity?.Name;
            var today = DateOnly.FromDateTime(DateTime.Today);

            var upcoming = await _context.Reservations
                .Include(r => r.Court)
                .Where(r =>
                    r.UserId == userId &&
                    r.Date >= today &&
                    r.Status == ReservationStatus.Confirmed)
                .OrderBy(r => r.Date)
                .Take(3)
                .Select(r => new ReservationCardViewModel
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
                })
                .ToListAsync();

            return View(upcoming);
        }

        [AllowAnonymous]
        public IActionResult NotFound()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}