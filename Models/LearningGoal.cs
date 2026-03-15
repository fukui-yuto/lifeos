using System.ComponentModel.DataAnnotations.Schema;

namespace LifeOS.Models;

public class LearningGoal
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? TargetDate { get; set; }
    public bool IsCompleted { get; set; } = false;
    public bool IsArchived { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [NotMapped]
    public List<StickyNote> LinkedNotes { get; set; } = new();
}
