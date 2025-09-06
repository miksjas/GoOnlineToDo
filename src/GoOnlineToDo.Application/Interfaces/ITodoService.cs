using GoOnlineToDo.Application.Contracts;

namespace GoOnlineToDo.Application.Interfaces;

public interface ITodoService
{
    Task<List<TodoDto>> GetAllAsync();
    Task<TodoDto?> GetByIdAsync(int id);
    Task<List<TodoDto>> GetUpcomingAsync();
    Task<TodoDto> CreateAsync(CreateTodoRequest request);
    Task<TodoDto?> UpdateAsync(int id, UpdateTodoRequest request);
    Task<TodoDto?> UpdatePercentAsync(int id, int percentComplete);
    Task<bool> DeleteAsync(int id);
    Task<TodoDto?> MarkDoneAsync(int id);
}