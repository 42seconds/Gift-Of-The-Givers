using gift_of_the_givers.Models;
using gift_of_the_givers.Services;

namespace gift_of_the_givers.Data;

public static class SeedData
{
    // Seeds a demo employee and donor so the prototype can be exercised immediately after startup.
    public static async Task InitializeAsync(ApplicationDbContext db, AccountService accountService)
    {
        await accountService.EnsureSeedRolesAsync();

        const string employeeEmail = "employee@giftofthegivers.org";
        var employee = await db.FindUserByEmailAsync(employeeEmail);
        if (employee is null)
        {
            employee = new ApplicationUser
            {
                UserName = employeeEmail,
                Email = employeeEmail,
                FirstName = "Thandiwe",
                LastName = "Nkosi",
                EmailConfirmed = true
            };

            await accountService.CreateUserAsync(employee, "Employee@123", "Employee");
        }

        const string donorEmail = "donor@example.com";
        var donor = await db.FindUserByEmailAsync(donorEmail);
        if (donor is null)
        {
            donor = new ApplicationUser
            {
                UserName = donorEmail,
                Email = donorEmail,
                FirstName = "Sipho",
                LastName = "Mokoena",
                EmailConfirmed = true
            };

            await accountService.CreateUserAsync(donor, "Donor@123", "Donor");
        }

        if (await db.CountProjectUpdatesAsync() == 0)
        {
            await db.InsertProjectUpdateAsync(new ProjectUpdate
            {
                Title = "KwaZulu-Natal flood relief",
                Description = "Prototype update showing how employee notes will appear in the dashboard. Volunteers with logistics and driving experience are still needed.",
                PostedByUserId = employee.Id,
                PostedByName = employee.DisplayName,
                PostedOn = DateTime.UtcNow.AddDays(-2)
            });
        }
    }
}
