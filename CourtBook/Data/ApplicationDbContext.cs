using CourtBook.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CourtBook.Data
{
    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Court> Courts { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(
            ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Court configuration
            builder.Entity<Court>(entity =>
            {
                entity.HasKey(c => c.Id);

                entity.Property(c => c.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(c => c.Name)
                    .IsUnique();

                entity.Property(c => c.SportType)
                    .IsRequired()
                    .HasConversion<int>();

                entity.Property(c => c.OperatingHours)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(c => c.PricePerHour)
                    .IsRequired()
                    .HasColumnType("decimal(10,2)");

                entity.Property(c => c.IsActive)
                    .HasDefaultValue(true);
            });

            // Reservation configuration
            builder.Entity<Reservation>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.StartTime)
                    .IsRequired()
                    .HasMaxLength(5);

                entity.Property(r => r.EndTime)
                    .IsRequired()
                    .HasMaxLength(5);

                entity.Property(r => r.TotalAmount)
                    .HasColumnType("decimal(10,2)");

                entity.Property(r => r.Status)
                    .HasConversion<int>()
                    .HasDefaultValue(
                        ReservationStatus.Confirmed);

                entity.Property(r => r.PaymentStatus)
                    .HasConversion<int>()
                    .HasDefaultValue(
                        PaymentStatus.Unpaid);

                entity.Property(r => r.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                // Reservation → User
                entity.HasOne(r => r.User)
                    .WithMany(u => u.Reservations)
                    .HasForeignKey(r => r.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Reservation → Court
                entity.HasOne(r => r.Court)
                    .WithMany(c => c.Reservations)
                    .HasForeignKey(r => r.CourtId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Composite index for conflict detection
                entity.HasIndex(r => new
                {
                    r.CourtId,
                    r.Date,
                    r.StartTime,
                    r.Status
                }).HasDatabaseName(
                    "IX_Reservations_CourtId_Date_StartTime_Status");
            });
        }
    }
}