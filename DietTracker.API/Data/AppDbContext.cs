using DietTracker.API.Models;
using Microsoft.EntityFrameworkCore;

namespace DietTracker.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }
    public DbSet<DietPlan> DietPlans { get; set; }
    public DbSet<MealSlot> MealSlots { get; set; }
    public DbSet<FoodOption> FoodOptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── User ──────────────────────────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email)
                  .IsUnique()
                  .HasDatabaseName("UQ_Users_Email");

            entity.Property(u => u.FirstName).HasMaxLength(100);
            entity.Property(u => u.MiddleName).HasMaxLength(100);
            entity.Property(u => u.LastName).HasMaxLength(100);

            entity.Property(u => u.HeightCm).HasColumnType("decimal(5,2)");
            entity.Property(u => u.WeightKg).HasColumnType("decimal(5,2)");
            entity.Property(u => u.GoalWeightKg).HasColumnType("decimal(5,2)");

            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(u => u.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── DietPlan ──────────────────────────────────────────────────────────
        modelBuilder.Entity<DietPlan>(entity =>
        {
            entity.HasIndex(dp => dp.UserId)
                  .HasDatabaseName("IX_DietPlans_UserId");

            entity.HasOne(dp => dp.User)
                  .WithMany()
                  .HasForeignKey(dp => dp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(dp => dp.MealSlots)
                  .WithOne(ms => ms.DietPlan)
                  .HasForeignKey(ms => ms.DietPlanId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(dp => dp.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(dp => dp.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── MealSlot ──────────────────────────────────────────────────────────
        modelBuilder.Entity<MealSlot>(entity =>
        {
            entity.HasIndex(ms => ms.DietPlanId)
                  .HasDatabaseName("IX_MealSlots_DietPlanId");

            entity.HasMany(ms => ms.FoodOptions)
                  .WithOne(fo => fo.MealSlot)
                  .HasForeignKey(fo => fo.MealSlotId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.Property(ms => ms.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(ms => ms.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── FoodOption ────────────────────────────────────────────────────────
        modelBuilder.Entity<FoodOption>(entity =>
        {
            entity.HasIndex(fo => fo.MealSlotId)
                  .HasDatabaseName("IX_FoodOptions_MealSlotId");

            entity.Property(fo => fo.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(fo => fo.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
