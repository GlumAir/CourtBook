using CourtBook.Models;
using CourtBook.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminUsersController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
    string search = null)
        {
            var usersQuery = _userManager.Users.AsQueryable();

            if (!string.IsNullOrEmpty(search))
                usersQuery = usersQuery.Where(u =>
                    u.FullName.Contains(search) ||
                    u.Email.Contains(search));

            var allUsers = await usersQuery
                .OrderBy(u => u.FullName)
                .ToListAsync();

            // Exclude admins
            var result = new List<AdminUserRowViewModel>();
            foreach (var u in allUsers)
            {
                var roles = await _userManager.GetRolesAsync(u);
                if (roles.Contains("Admin")) continue;

                result.Add(new AdminUserRowViewModel
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    CreatedAt = u.CreatedAt,
                    IsActive = u.LockoutEnd == null ||
                               u.LockoutEnd < DateTimeOffset.UtcNow
                });
            }

            ViewBag.Search = search;
            ViewBag.ActiveCount = result.Count(u => u.IsActive);
            ViewBag.DeactivatedCount = result.Count(u => !u.IsActive);

            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Deactivate(
            string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEnabledAsync(
                user, true);
            await _userManager.SetLockoutEndDateAsync(
                user, DateTimeOffset.MaxValue);

            TempData["Success"] =
                "User deactivated successfully.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Reactivate(
            string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            await _userManager.SetLockoutEndDateAsync(
                user, null);

            TempData["Success"] =
                "User reactivated successfully.";

            return RedirectToAction("Index");
        }
    }
}