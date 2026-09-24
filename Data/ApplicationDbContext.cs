using GiftOfTheGivers.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GiftOfTheGivers.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Donation> Donations { get; set; } = default!;
        public DbSet<VolunteerSignup> VolunteerSignups { get; set; } = default!;
        public DbSet<ProjectUpdate> ProjectUpdates { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Donation>()
                .Property(d => d.Amount)
                .HasColumnType("decimal(18,2)");
        }
    }
}
