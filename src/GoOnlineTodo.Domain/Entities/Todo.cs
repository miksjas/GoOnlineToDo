namespace GoOnlineToDo.Domain.Entities;

public class Todo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DueDate { get; set; }
    public int PercentComplete { get; set; }
    public bool IsDone { get; set; }
}