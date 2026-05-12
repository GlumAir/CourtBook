using CourtBook.Models;
using Microsoft.AspNetCore.Identity;

namespace CourtBook.Data
{
    public class DatabaseSeeder
    {
        private readonly UserManager<ApplicationUser>
            _userManager;
        private readonly RoleManager<IdentityRole>
            _roleManager;
        private readonly ApplicationDbContext _context;

        public DatabaseSeeder(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedRolesAsync();
            await SeedAdminAsync();
            await SeedCourtsAsync();
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = { "Admin", "User" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(
                        new IdentityRole(role));
                }
            }
        }

        private async Task SeedAdminAsync()
        {
            const string adminEmail =
                "admin@courtbook.com";
            const string adminPassword =
                "Admin@123456";

            if (await _userManager
                    .FindByEmailAsync(adminEmail) != null)
                return;

            var admin = new ApplicationUser
            {
                FullName = "System Administrator",
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true
            };

            var result = await _userManager
                .CreateAsync(admin, adminPassword);

            if (result.Succeeded)
            {
                await _userManager
                    .AddToRoleAsync(admin, "Admin");
            }
        }

        private async Task SeedCourtsAsync()
        {
            if (_context.Courts.Any()) return;

            var courts = new List<Court>
            {
                new Court
                {
                    Name = "Badminton Court A",
                    SportType = SportType.Badminton,
                    OperatingHours = "6:00 AM - 10:00 PM",
                    PricePerHour = 150,
                    IsActive = true
                },
                new Court
                {
                    Name = "Badminton Court B",
                    SportType = SportType.Badminton,
                    OperatingHours = "6:00 AM - 10:00 PM",
                    PricePerHour = 150,
                    IsActive = true
                },
                new Court
                {
                    Name = "Pickleball Court A",
                    SportType = SportType.Pickleball,
                    OperatingHours = "7:00 AM - 9:00 PM",
                    PricePerHour = 200,
                    IsActive = true
                },
                new Court
                {
                    Name = "Pickleball Court B",
                    SportType = SportType.Pickleball,
                    OperatingHours = "7:00 AM - 9:00 PM",
                    PricePerHour = 200,
                    IsActive = true
                }
            };

            _context.Courts.AddRange(courts);
            await _context.SaveChangesAsync();
        }
    }
}