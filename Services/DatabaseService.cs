using LifeOS.Data;
using LifeOS.Models;
using Microsoft.EntityFrameworkCore;

// アプリ名定数はAppDbContextで管理

namespace LifeOS.Services;

public class DatabaseService
{
    public DatabaseService()
    {
        using var db = CreateContext();
        db.Database.EnsureCreated();
        Migrate(db);
    }

    private static void Migrate(AppDbContext db)
    {
        // スキーマ変更を安全に適用
        var migrations = new[]
        {
            "ALTER TABLE LearningGoals ADD COLUMN StickyNoteId INTEGER NULL REFERENCES StickyNotes(Id)",
            "CREATE TABLE IF NOT EXISTS OperationLogs (Id INTEGER PRIMARY KEY AUTOINCREMENT, OccurredAt TEXT NOT NULL, Action TEXT NOT NULL, Detail TEXT NOT NULL DEFAULT '')",
            "CREATE TABLE IF NOT EXISTS GoalNotes (LearningGoalId INTEGER NOT NULL, StickyNoteId INTEGER NOT NULL, PRIMARY KEY (LearningGoalId, StickyNoteId))",
            "ALTER TABLE LearningGoals DROP COLUMN StickyNoteId",
        };
        foreach (var sql in migrations)
        {
            try { db.Database.ExecuteSqlRaw(sql); }
            catch { /* 列が既に存在する場合は無視 */ }
        }
    }

    private static AppDbContext CreateContext() => new();

    // --- StickyNote ---
    public List<StickyNote> GetAllNotes()
    {
        using var db = CreateContext();
        return db.StickyNotes.Include(n => n.Tasks).AsNoTracking().ToList();
    }

    public StickyNote AddNote(StickyNote note)
    {
        using var db = CreateContext();
        db.StickyNotes.Add(note);
        db.SaveChanges();
        return note;
    }

    public void UpdateNote(StickyNote note)
    {
        using var db = CreateContext();
        note.UpdatedAt = DateTime.Now;
        db.StickyNotes.Update(note);
        db.SaveChanges();
    }

    public void DeleteNote(int id)
    {
        using var db = CreateContext();
        var note = db.StickyNotes.Include(n => n.Tasks).FirstOrDefault(n => n.Id == id);
        if (note != null)
        {
            db.StickyNotes.Remove(note);
            db.SaveChanges();
        }
    }

    // --- TaskItem ---
    public List<TaskItem> GetAllTasks()
    {
        using var db = CreateContext();
        return db.Tasks.Where(t => !t.IsArchived).AsNoTracking().ToList();
    }

    public TaskItem AddTask(TaskItem task)
    {
        using var db = CreateContext();
        db.Tasks.Add(task);
        db.SaveChanges();
        return task;
    }

    public void UpdateTask(TaskItem task)
    {
        using var db = CreateContext();
        db.Tasks.Update(task);
        db.SaveChanges();
    }

    public void DeleteTask(int id)
    {
        using var db = CreateContext();
        var task = db.Tasks.Find(id);
        if (task != null)
        {
            db.Tasks.Remove(task);
            db.SaveChanges();
        }
    }

    // --- LearningGoal ---
    public List<LearningGoal> GetAllGoals()
    {
        using var db = CreateContext();
        var goals = db.LearningGoals
            .Where(g => !g.IsArchived)
            .AsNoTracking()
            .ToList();

        var goalNotes = db.GoalNotes.AsNoTracking().ToList();
        var noteIds = goalNotes.Select(gn => gn.StickyNoteId).Distinct().ToList();
        var notes = db.StickyNotes
            .Where(n => noteIds.Contains(n.Id))
            .AsNoTracking()
            .ToList();

        foreach (var goal in goals)
        {
            var linked = goalNotes.Where(gn => gn.LearningGoalId == goal.Id)
                                  .Select(gn => notes.FirstOrDefault(n => n.Id == gn.StickyNoteId))
                                  .Where(n => n != null)
                                  .ToList();
            goal.LinkedNotes = linked!;
        }

        return goals;
    }

    public void UpdateGoalNotes(int goalId, List<int> noteIds)
    {
        using var db = CreateContext();
        var existing = db.GoalNotes.Where(gn => gn.LearningGoalId == goalId).ToList();
        db.GoalNotes.RemoveRange(existing);
        foreach (var nid in noteIds)
            db.GoalNotes.Add(new GoalNote { LearningGoalId = goalId, StickyNoteId = nid });
        db.SaveChanges();
    }

    public LearningGoal AddGoal(LearningGoal goal)
    {
        using var db = CreateContext();
        db.LearningGoals.Add(goal);
        db.SaveChanges();
        return goal;
    }

    public void UpdateGoal(LearningGoal goal)
    {
        using var db = CreateContext();
        var existing = db.LearningGoals.Find(goal.Id);
        if (existing == null) return;
        existing.Title = goal.Title;
        existing.Description = goal.Description;
        existing.TargetDate = goal.TargetDate;
        existing.IsCompleted = goal.IsCompleted;
        db.SaveChanges();
    }

    public void DeleteGoal(int id)
    {
        using var db = CreateContext();
        var goal = db.LearningGoals.Find(id);
        if (goal != null)
        {
            db.LearningGoals.Remove(goal);
            db.SaveChanges();
        }
    }

    // --- LearningLog ---
    public List<LearningLog> GetAllLogs()
    {
        using var db = CreateContext();
        return db.LearningLogs.OrderByDescending(l => l.LoggedAt).AsNoTracking().ToList();
    }

    public LearningLog AddLog(LearningLog log)
    {
        using var db = CreateContext();
        db.LearningLogs.Add(log);
        db.SaveChanges();
        return log;
    }

    public void DeleteLog(int id)
    {
        using var db = CreateContext();
        var log = db.LearningLogs.Find(id);
        if (log != null)
        {
            db.LearningLogs.Remove(log);
            db.SaveChanges();
        }
    }

    // --- OperationLog ---
    public void AddOperationLog(string action, string detail = "")
    {
        using var db = CreateContext();
        db.OperationLogs.Add(new OperationLog { Action = action, Detail = detail });
        db.SaveChanges();
    }

    public List<OperationLog> GetOperationLogs(int limit = 200)
    {
        using var db = CreateContext();
        return db.OperationLogs
            .OrderByDescending(l => l.OccurredAt)
            .Take(limit)
            .AsNoTracking()
            .ToList();
    }

    public Dictionary<DateTime, int> GetHeatmapData(int year)
    {
        using var db = CreateContext();
        return db.LearningLogs
            .Where(l => l.LoggedAt.Year == year)
            .GroupBy(l => l.LoggedAt.Date)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.DurationMinutes));
    }
}
