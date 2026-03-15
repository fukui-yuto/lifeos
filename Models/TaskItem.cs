namespace LifeOS.Models;

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public Priority Priority { get; set; } = Priority.Medium;
    public DateTime? DueDate { get; set; }
    public string Category { get; set; } = string.Empty;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public int? StickyNoteId { get; set; }
    public StickyNote? StickyNote { get; set; }
}

public enum Priority
{
    Low,
    Medium,
    High
}
