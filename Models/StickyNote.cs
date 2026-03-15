namespace LifeOS.Models;

public class StickyNote
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFF176";
    public double PositionX { get; set; } = 100;
    public double PositionY { get; set; } = 100;
    public double Width { get; set; } = 220;
    public double Height { get; set; } = 180;
    public NoteType NoteType { get; set; } = NoteType.Memo;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    public List<TaskItem> Tasks { get; set; } = new();
}

public enum NoteType
{
    Memo,
    Task,
    Goal
}
