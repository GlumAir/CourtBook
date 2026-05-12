using CourtBook.Data;
using CourtBook.Models;
using CourtBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCourtsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminCourtsController(
            ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            string sport = null)
        {
            var query = _context.Courts.AsQueryable();

            if (!string.IsNullOrEmpty(sport) &&
                Enum.TryParse<SportType>(sport, out var sportType))
                query = query.Where(c =>
                    c.SportType == sportType);

            var courts = await query.ToListAsync();
            ViewBag.SelectedSport = sport;
            return View(courts);
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(
            CreateCourtViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var court = new Court
            {
                Name = model.Name,
                SportType = model.SportType,
                OperatingHours = model.OperatingHours,
                PricePerHour = model.PricePerHour,
                IsActive = true
            };

            _context.Courts.Add(court);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Court created successfully.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var court = await _context.Courts.FindAsync(id);
            if (court == null) return NotFound();

            return View(new EditCourtViewModel
            {
                Id = court.Id,
                Name = court.Name,
                SportType = court.SportType,
                OperatingHours = court.OperatingHours,
                PricePerHour = court.PricePerHour
            });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(
            EditCourtViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var court = await _context.Courts
                .FindAsync(model.Id);

            if (court == null) return NotFound();

            court.Name = model.Name;
            court.SportType = model.SportType;
            court.OperatingHours = model.OperatingHours;
            court.PricePerHour = model.PricePerHour;

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Court updated successfully.";

            return RedirectToAction("Index");
        }



        [HttpPost]
        public async Task<IActionResult> Activate(int id)
        {
            var court = await _context.Courts.FindAsync(id);
            if (court == null) return NotFound();

            court.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Court activated successfully.";
            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> Deactivate(int id)
        {
            var court = await _context.Courts.FindAsync(id);
            if (court == null) return NotFound();

            court.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Court deactivated successfully.";

            return RedirectToAction("Index");
        }
    }
}