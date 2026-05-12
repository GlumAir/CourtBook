using CourtBook.Data;
using CourtBook.Models;
using CourtBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Controllers
{
    [Authorize(Roles = "User")]
    public class CourtsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CourtsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string sport = null)
        {
            var query = _context.Courts
                .Where(c => c.IsActive);

            if (!string.IsNullOrEmpty(sport) &&
                Enum.TryParse<SportType>(sport, out var sportType))
                query = query.Where(c =>
                    c.SportType == sportType);

            var courts = await query
                .Select(c => new CourtCardViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    SportType = c.SportType,
                    OperatingHours = c.OperatingHours,
                    PricePerHour = c.PricePerHour,
                    IsActive = c.IsActive
                })
                .ToListAsync();

            return View(new CourtListViewModel
            {
                Courts = courts,
                SelectedSport = sport
            });
        }
    }
}