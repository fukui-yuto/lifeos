namespace LifeOS.Models;

public class OperationLog
{
    public int Id { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string Action { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
}
