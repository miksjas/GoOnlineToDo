namespace GoOnlineToDo.Application.Contracts;

public record UpdateTodoWithIdRequest(
    int Id,
    string Title,
    string? Description,
    DateTime DueDate,
    int PercentComplete,
    bool IsDone)
{
    public UpdateTodoRequest ToUpdateTodoRequest() =>
        new(Title, Description, DueDate, PercentComplete, IsDone);
}

