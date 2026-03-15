using LifeOS.Models;
using Microsoft.EntityFrameworkCore;
using System.IO;

namespace LifeOS.Data;

public class AppDbContext : DbContext
{
    public DbSet<StickyNote> StickyNotes => Set<StickyNote>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<LearningGoal> LearningGoals => Set<LearningGoal>();
    public DbSet<LearningLog> LearningLogs => Set<LearningLog>();
    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<GoalNote> GoalNotes => Set<GoalNote>();

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var dbPath = Path.Combine(appData, "LifeOS", "lifeos.db"); // "LifeOS" = アプリ名
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        options.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StickyNote>()
            .HasMany(n => n.Tasks)
            .WithOne(t => t.StickyNote)
            .HasForeignKey(t => t.StickyNoteId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<GoalNote>().HasKey(gn => new { gn.LearningGoalId, gn.StickyNoteId });
        modelBuilder.Entity<GoalNote>()
            .HasOne<LearningGoal>().WithMany().HasForeignKey(gn => gn.LearningGoalId);
        modelBuilder.Entity<GoalNote>()
            .HasOne<StickyNote>().WithMany().HasForeignKey(gn => gn.StickyNoteId);

        modelBuilder.Entity<LearningGoal>()
            .HasMany<LearningLog>()
            .WithOne(l => l.LearningGoal)
            .HasForeignKey(l => l.LearningGoalId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
