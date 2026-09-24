using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Identity;

namespace GiftOfTheGivers.Data
{
    // Runs at startup to guarantee roles + at least one demo login of each type exist,
    // so markers/testers can log in immediately without manual setup.
    public static class SeedData
    {
        public static async Task InitializeAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();

            string[] roles = { "Employee", "Donor" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Demo Employee account
            var employeeEmail = "employee@giftofthegivers.org";
            if (await userManager.FindByEmailAsync(employeeEmail) is null)
            {
                var employee = new ApplicationUser
                {
                    UserName = employeeEmail,
                    Email = employeeEmail,
                    FullName = "Thandiwe Nkosi",
                    Department = "Relief Operations",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(employee, "Employee@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(employee, "Employee");
            }

            // Demo Donor account
            var donorEmail = "donor@example.com";
            if (await userManager.FindByEmailAsync(donorEmail) is null)
            {
                var donor = new ApplicationUser
                {
                    UserName = donorEmail,
                    Email = donorEmail,
                    FullName = "Sipho Mokoena",
                    EmailConfirmed = true
                };
                var result = await userManager.CreateAsync(donor, "Donor@123");
                if (result.Succeeded)
                    await userManager.AddToRoleAsync(donor, "Donor");
            }

            // Sample relief project update so the homepage/dashboard isn't empty
            if (!db.ProjectUpdates.Any())
            {
                db.ProjectUpdates.Add(new ProjectUpdate
                {
                    Title = "KwaZulu-Natal Flood Relief - Phase 2",
                    Description = "Distribution of food parcels and clean water continues across " +
                        "affected communities. Over 3,000 households reached so far. Volunteers " +
                        "with logistics and driving experience are still urgently needed.",
                    PostedByUserId = "seed",
                    PostedByName = "Thandiwe Nkosi",
                    PostedOn = DateTime.UtcNow.AddDays(-2)
                });
                await db.SaveChangesAsync();
            }
        }
    }
}
