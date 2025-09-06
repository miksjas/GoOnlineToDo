namespace GoOnlineToDo.Application.Contracts;

public record UpdateTodoRequest(
    string Title,
    string? Description,
    DateTime DueDate,
    int PercentComplete,
    bool IsDone
);