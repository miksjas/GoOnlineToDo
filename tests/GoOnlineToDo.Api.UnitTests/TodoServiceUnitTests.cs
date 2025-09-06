using FluentAssertions;
using GoOnlineToDo.Domain.Entities;
using GoOnlineToDo.Application.Contracts;
using GoOnlineToDo.Infrastructure.Data;
using GoOnlineToDo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GoOnline.ToDo.Api.UnitTests;

public class TodoServiceUnitTests : IDisposable
{
    private ToDoDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<ToDoDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ToDoDbContext(options);
    }

    public void Dispose()
    {
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyList_WhenNoTodos()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        var result = await service.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllTodos_WhenTodosExist()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todos = new[]
        {
            new Todo { Title = "Todo 1", Description = "Description 1", DueDate = DateTime.Today.AddDays(1), PercentComplete = 0, IsDone = false },
            new Todo { Title = "Todo 2", Description = "Description 2", DueDate = DateTime.Today.AddDays(2), PercentComplete = 50, IsDone = false },
            new Todo { Title = "Todo 3", Description = "Description 3", DueDate = DateTime.Today.AddDays(3), PercentComplete = 100, IsDone = true }
        };

        context.Todos.AddRange(todos);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().OnlyHaveUniqueItems(x => x.Id);
        result.Should().Contain(t => t.Title == "Todo 1");
        result.Should().Contain(t => t.Title == "Todo 2");
        result.Should().Contain(t => t.Title == "Todo 3");
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNull_WhenTodoDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        var result = await service.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsTodo_WhenTodoExists()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todo = new Todo 
        { 
            Title = "Test Todo", 
            Description = "Test Description", 
            DueDate = DateTime.Today.AddDays(1), 
            PercentComplete = 25, 
            IsDone = false 
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetByIdAsync(todo.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(todo.Id);
        result.Title.Should().Be("Test Todo");
        result.Description.Should().Be("Test Description");
        result.DueDate.Should().Be(DateTime.Today.AddDays(1));
        result.PercentComplete.Should().Be(25);
        result.IsDone.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_CreatesAndReturnsTodo()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var request = new CreateTodoRequest("New Todo", "New Description", DateTime.Today.AddDays(5));

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Title.Should().Be("New Todo");
        result.Description.Should().Be("New Description");
        result.DueDate.Should().Be(DateTime.Today.AddDays(5));
        result.PercentComplete.Should().Be(0);
        result.IsDone.Should().BeFalse();

        // Verify it was saved to database
        var savedTodo = await context.Todos.FindAsync(result.Id);
        savedTodo.Should().NotBeNull();
        savedTodo!.Title.Should().Be("New Todo");
    }

    [Fact]
    public async Task CreateAsync_SetsDefaultValues()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var request = new CreateTodoRequest("Minimal Todo", null, DateTime.Today.AddDays(1));

        // Act
        var result = await service.CreateAsync(request);

        // Assert
        result.PercentComplete.Should().Be(0);
        result.IsDone.Should().BeFalse();
        result.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ReturnsNull_WhenTodoDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var request = new UpdateTodoRequest("Updated Title", "Updated Description", DateTime.Today.AddDays(3), 75, true);

        // Act
        var result = await service.UpdateAsync(999, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAndReturnsTodo_WhenTodoExists()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todo = new Todo 
        { 
            Title = "Original Title", 
            Description = "Original Description", 
            DueDate = DateTime.Today.AddDays(1), 
            PercentComplete = 0, 
            IsDone = false 
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        var request = new UpdateTodoRequest("Updated Title", "Updated Description", DateTime.Today.AddDays(3), 75, true);

        // Act
        var result = await service.UpdateAsync(todo.Id, request);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(todo.Id);
        result.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Updated Description");
        result.DueDate.Should().Be(DateTime.Today.AddDays(3));
        result.PercentComplete.Should().Be(75);
        result.IsDone.Should().BeTrue();

        // Verify database was updated
        var updatedTodo = await context.Todos.FindAsync(todo.Id);
        updatedTodo!.Title.Should().Be("Updated Title");
        updatedTodo.PercentComplete.Should().Be(75);
        updatedTodo.IsDone.Should().BeTrue();
    }

    [Fact]
    public async Task UpdatePercentAsync_ReturnsNull_WhenTodoDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        var result = await service.UpdatePercentAsync(999, 50);

        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePercentAsync_UpdatesPercentAndReturnsTodo_WhenTodoExists()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todo = new Todo 
        { 
            Title = "Test Todo", 
            Description = "Test Description", 
            DueDate = DateTime.Today.AddDays(1), 
            PercentComplete = 25, 
            IsDone = false 
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        // Act
        var result = await service.UpdatePercentAsync(todo.Id, 80);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(todo.Id);
        result.PercentComplete.Should().Be(80);
        result.Title.Should().Be("Test Todo"); // Other properties unchanged

        // Verify database was updated
        var updatedTodo = await context.Todos.FindAsync(todo.Id);
        updatedTodo!.PercentComplete.Should().Be(80);
        updatedTodo.IsDone.Should().BeFalse(); // Should remain unchanged
    }

    [Fact]
    public async Task DeleteAsync_ReturnsFalse_WhenTodoDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        var result = await service.DeleteAsync(999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_ReturnsTrue_WhenTodoExists()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todo = new Todo 
        { 
            Title = "Todo to Delete", 
            Description = "Will be deleted", 
            DueDate = DateTime.Today.AddDays(1), 
            PercentComplete = 50, 
            IsDone = false 
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(todo.Id);

        // Assert
        result.Should().BeTrue();

        // Verify todo was removed from database
        var deletedTodo = await context.Todos.FindAsync(todo.Id);
        deletedTodo.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_RemovesOnlySpecifiedTodo()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todoToKeep = new Todo { Title = "Keep This", DueDate = DateTime.Today.AddDays(1), PercentComplete = 0, IsDone = false };
        var todoToDelete = new Todo { Title = "Delete This", DueDate = DateTime.Today.AddDays(2), PercentComplete = 0, IsDone = false };

        context.Todos.AddRange(todoToKeep, todoToDelete);
        await context.SaveChangesAsync();

        // Act
        var result = await service.DeleteAsync(todoToDelete.Id);

        // Assert
        result.Should().BeTrue();

        var remainingTodo = await context.Todos.FindAsync(todoToKeep.Id);
        remainingTodo.Should().NotBeNull();
        remainingTodo!.Title.Should().Be("Keep This");

        var deletedTodo = await context.Todos.FindAsync(todoToDelete.Id);
        deletedTodo.Should().BeNull();
    }

    [Fact]
    public async Task MarkDoneAsync_ReturnsNull_WhenTodoDoesNotExist()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        var result = await service.MarkDoneAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task MarkDoneAsync_MarksAsDoneAndSetsPercent100_WhenTodoExists()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todo = new Todo 
        { 
            Title = "Todo to Complete", 
            Description = "Will be marked done", 
            DueDate = DateTime.Today.AddDays(1), 
            PercentComplete = 75, 
            IsDone = false 
        };

        context.Todos.Add(todo);
        await context.SaveChangesAsync();

        // Act
        var result = await service.MarkDoneAsync(todo.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(todo.Id);
        result.IsDone.Should().BeTrue();
        result.PercentComplete.Should().Be(100);
        result.Title.Should().Be("Todo to Complete"); // Other properties unchanged

        // Verify database was updated
        var updatedTodo = await context.Todos.FindAsync(todo.Id);
        updatedTodo!.IsDone.Should().BeTrue();
        updatedTodo.PercentComplete.Should().Be(100);
    }

    [Fact]
    public async Task GetUpcomingAsync_ReturnsEmptyList_WhenNoUpcomingTodos()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange - add todos outside the upcoming range
        var pastTodo = new Todo { Title = "Past Todo", DueDate = DateTime.Today.AddDays(-1), PercentComplete = 0, IsDone = false };
        var futureTodo = new Todo { Title = "Future Todo", DueDate = DateTime.Today.AddDays(10), PercentComplete = 0, IsDone = false };

        context.Todos.AddRange(pastTodo, futureTodo);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetUpcomingAsync_ReturnsUpcomingTodos_WithinNext7Days()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange
        var todayTodo = new Todo { Title = "Today Todo", DueDate = DateTime.Today, PercentComplete = 0, IsDone = false };
        var tomorrowTodo = new Todo { Title = "Tomorrow Todo", DueDate = DateTime.Today.AddDays(1), PercentComplete = 25, IsDone = false };
        var weekAheadTodo = new Todo { Title = "Week Ahead Todo", DueDate = DateTime.Today.AddDays(7), PercentComplete = 50, IsDone = false };
        var pastTodo = new Todo { Title = "Past Todo", DueDate = DateTime.Today.AddDays(-1), PercentComplete = 0, IsDone = false };
        var farFutureTodo = new Todo { Title = "Far Future Todo", DueDate = DateTime.Today.AddDays(8), PercentComplete = 0, IsDone = false };

        context.Todos.AddRange(todayTodo, tomorrowTodo, weekAheadTodo, pastTodo, farFutureTodo);
        await context.SaveChangesAsync();

        // Act
        var result = await service.GetUpcomingAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result.Should().Contain(t => t.Title == "Today Todo");
        result.Should().Contain(t => t.Title == "Tomorrow Todo");
        result.Should().Contain(t => t.Title == "Week Ahead Todo");
        result.Should().NotContain(t => t.Title == "Past Todo");
        result.Should().NotContain(t => t.Title == "Far Future Todo");
    }
}