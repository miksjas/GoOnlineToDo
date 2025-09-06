namespace GoOnlineToDo.Application.Contracts;

public record CreateTodoRequest(
    string Title,
    string? Description,
    DateTime DueDate
);