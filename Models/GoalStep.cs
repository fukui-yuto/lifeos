namespace LifeOS.Models;

public class GoalStep
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public int Order { get; set; } = 0;

    public int LearningGoalId { get; set; }
    public LearningGoal? LearningGoal { get; set; }
}
