namespace LifeOS.Models;

public class LearningLog
{
    public int Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public int DurationMinutes { get; set; } = 0;
    public DateTime LoggedAt { get; set; } = DateTime.Now;

    public int? LearningGoalId { get; set; }
    public LearningGoal? LearningGoal { get; set; }
}
