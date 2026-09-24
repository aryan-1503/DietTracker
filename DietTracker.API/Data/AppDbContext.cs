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
    public DbSet<DailyEntry> DailyEntries { get; set; }
    public DbSet<UserSettings> UserSettings { get; set; }
    public DbSet<ReminderLog> ReminderLogs { get; set; }
    public DbSet<DailyNote> DailyNotes { get; set; }
    public DbSet<WeeklyWeight> WeeklyWeights { get; set; }

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

            entity.Property(dp => dp.StartDate).HasColumnType("date");

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

        // ── UserSettings ──────────────────────────────────────────────────────
        modelBuilder.Entity<UserSettings>(entity =>
        {
            entity.HasIndex(us => us.UserId).IsUnique().HasDatabaseName("UQ_UserSettings_UserId");
            entity.HasOne(us => us.User).WithOne()
                  .HasForeignKey<UserSettings>(us => us.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(us => us.ReminderTime).HasDefaultValue("21:00");
            entity.Property(us => us.TimeZoneId).HasDefaultValue("UTC");
            entity.Property(us => us.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(us => us.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── DailyEntry ────────────────────────────────────────────────────────
        modelBuilder.Entity<DailyEntry>(entity =>
        {
            entity.HasIndex(de => new { de.UserId, de.EntryDate })
                  .HasDatabaseName("IX_DailyEntries_UserId_EntryDate");
            entity.HasIndex(de => new { de.UserId, de.EntryDate, de.MealSlotId })
                  .IsUnique().HasDatabaseName("UQ_DailyEntries_UserDateSlot");
            entity.Property(de => de.EntryDate).HasColumnType("date");
            entity.HasOne(de => de.User).WithMany()
                  .HasForeignKey(de => de.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(de => de.DietPlan).WithMany()
                  .HasForeignKey(de => de.DietPlanId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(de => de.MealSlot).WithMany()
                  .HasForeignKey(de => de.MealSlotId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(de => de.FoodOption).WithMany()
                  .HasForeignKey(de => de.FoodOptionId).OnDelete(DeleteBehavior.SetNull)
                  .IsRequired(false);
            entity.Property(de => de.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(de => de.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── ReminderLog ───────────────────────────────────────────────────────
        modelBuilder.Entity<ReminderLog>(entity =>
        {
            entity.HasIndex(rl => new { rl.UserId, rl.ReminderDate })
                  .IsUnique().HasDatabaseName("UQ_ReminderLogs_UserDate");
            entity.Property(rl => rl.ReminderDate).HasColumnType("date");
            entity.HasOne(rl => rl.User).WithMany()
                  .HasForeignKey(rl => rl.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(rl => rl.SentAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── DailyNote ─────────────────────────────────────────────────────────
        modelBuilder.Entity<DailyNote>(entity =>
        {
            entity.HasIndex(dn => new { dn.UserId, dn.EntryDate })
                  .IsUnique().HasDatabaseName("UQ_DailyNotes_UserDate");
            entity.HasIndex(dn => dn.UserId).HasDatabaseName("IX_DailyNotes_UserId");
            entity.Property(dn => dn.EntryDate).HasColumnType("date");
            entity.Property(dn => dn.NoteText).HasMaxLength(1000);
            entity.HasOne(dn => dn.User).WithMany()
                  .HasForeignKey(dn => dn.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(dn => dn.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(dn => dn.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        // ── WeeklyWeight ──────────────────────────────────────────────────────
        modelBuilder.Entity<WeeklyWeight>(entity =>
        {
            entity.HasIndex(ww => new { ww.UserId, ww.Year, ww.WeekNumber })
                  .IsUnique().HasDatabaseName("UQ_WeeklyWeights_UserWeek");
            entity.HasIndex(ww => ww.UserId).HasDatabaseName("IX_WeeklyWeights_UserId");
            entity.Property(ww => ww.WeightKg).HasColumnType("decimal(5,1)");
            entity.HasOne(ww => ww.User).WithMany()
                  .HasForeignKey(ww => ww.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.Property(ww => ww.RecordedAt).HasDefaultValueSql("GETUTCDATE()");
        });
    }
}
