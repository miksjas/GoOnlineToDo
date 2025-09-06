using GoOnlineToDo.Domain.Entities;
using GoOnlineToDo.Application.Contracts;
using GoOnlineToDo.Application.Interfaces;
using GoOnlineToDo.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoOnlineToDo.Infrastructure.Services;

public class TodoService : ITodoService
{
    private readonly ToDoDbContext _db;

    public TodoService(ToDoDbContext db)
    {
        _db = db;
    }

    public async Task<List<TodoDto>> GetAllAsync() =>
        await _db.Todos.Select(t => ToDto(t)).ToListAsync();

    public async Task<TodoDto?> GetByIdAsync(int id) =>
        await _db.Todos.Where(t => t.Id == id).Select(t => ToDto(t)).FirstOrDefaultAsync();

    public async Task<List<TodoDto>> GetUpcomingAsync()
    {
        var today = DateTime.Today;
        var weekAhead = today.AddDays(7);

        return await _db.Todos
            .Where(t => t.DueDate >= today && t.DueDate <= weekAhead)
            .Select(t => ToDto(t))
            .ToListAsync();
    }

    public async Task<TodoDto> CreateAsync(CreateTodoRequest request)
    {
        var entity = new Todo
        {
            Title = request.Title,
            Description = request.Description,
            DueDate = request.DueDate,
            PercentComplete = 0,
            IsDone = false
        };

        _db.Todos.Add(entity);
        await _db.SaveChangesAsync();

        return ToDto(entity);
    }

    public async Task<TodoDto?> UpdateAsync(int id, UpdateTodoRequest request)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo == null) return null;

        todo.Title = request.Title;
        todo.Description = request.Description;
        todo.DueDate = request.DueDate;
        todo.PercentComplete = request.PercentComplete;
        todo.IsDone = request.IsDone;

        await _db.SaveChangesAsync();
        return ToDto(todo);
    }

    public async Task<TodoDto?> UpdatePercentAsync(int id, int percentComplete)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo == null) return null;

        todo.PercentComplete = percentComplete;

        await _db.SaveChangesAsync();
        return ToDto(todo);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo == null) return false;

        _db.Todos.Remove(todo);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<TodoDto?> MarkDoneAsync(int id)
    {
        var todo = await _db.Todos.FindAsync(id);
        if (todo == null) return null;

        todo.IsDone = true;
        todo.PercentComplete = 100;
        await _db.SaveChangesAsync();

        return ToDto(todo);
    }

    private static TodoDto ToDto(Todo t) =>
        new(t.Id, t.Title, t.Description, t.DueDate, t.PercentComplete, t.IsDone);
}