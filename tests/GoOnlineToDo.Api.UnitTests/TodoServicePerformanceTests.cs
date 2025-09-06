using System.Diagnostics;
using FluentAssertions;
using GoOnlineToDo.Domain.Entities;
using GoOnlineToDo.Application.Contracts;
using GoOnlineToDo.Infrastructure.Data;
using GoOnlineToDo.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

namespace GoOnline.ToDo.Api.UnitTests;

public class TodoServicePerformanceTests : IDisposable
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
    public async Task BulkCreateTodos_PerformanceTest()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);
        
        const int todoCount = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act - Create 1000 todos
        var tasks = new List<Task<TodoDto>>();
        for (int i = 0; i < todoCount; i++)
        {
            var request = new CreateTodoRequest(
                $"Performance Test Todo {i}",
                $"Description for todo {i}",
                DateTime.Today.AddDays(i % 30)
            );
            tasks.Add(service.CreateAsync(request));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var results = await Task.WhenAll(tasks);
        results.Should().HaveCount(todoCount);
        results.Should().OnlyHaveUniqueItems(x => x.Id);

        // Performance assertion - should complete within reasonable time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Creating 1000 todos should take less than 5 seconds");

        // Verify all todos were saved
        var allTodos = await service.GetAllAsync();
        allTodos.Should().HaveCount(todoCount);

        Console.WriteLine($"Created {todoCount} todos in {stopwatch.ElapsedMilliseconds}ms ({todoCount / (stopwatch.ElapsedMilliseconds / 1000.0):F2} todos/second)");
    }

    [Fact]
    public async Task BulkRetrieveTodos_PerformanceTest()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange - Pre-populate with 5000 todos
        const int todoCount = 5000;
        var todos = new List<Todo>();
        for (int i = 0; i < todoCount; i++)
        {
            todos.Add(new Todo
            {
                Title = $"Bulk Test Todo {i}",
                Description = $"Description {i}",
                DueDate = DateTime.Today.AddDays(i % 90),
                PercentComplete = i % 101,
                IsDone = i % 10 == 0
            });
        }

        context.Todos.AddRange(todos);
        await context.SaveChangesAsync();

        // Act - Measure retrieval time
        var stopwatch = Stopwatch.StartNew();
        var result = await service.GetAllAsync();
        stopwatch.Stop();

        // Assert
        result.Should().HaveCount(todoCount);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Retrieving 5000 todos should take less than 1 second");

        Console.WriteLine($"Retrieved {todoCount} todos in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task ConcurrentOperations_PerformanceTest()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange - Create some initial todos for update/delete operations
        var initialTodos = new List<TodoDto>();
        for (int i = 0; i < 100; i++)
        {
            var request = new CreateTodoRequest($"Initial Todo {i}", $"Description {i}", DateTime.Today.AddDays(1));
            var todo = await service.CreateAsync(request);
            initialTodos.Add(todo);
        }

        var stopwatch = Stopwatch.StartNew();

        // Act - Perform concurrent mixed operations
        var tasks = new List<Task>();

        // Concurrent creates
        for (int i = 0; i < 200; i++)
        {
            var request = new CreateTodoRequest($"Concurrent Todo {i}", $"Description {i}", DateTime.Today.AddDays(i % 7));
            tasks.Add(service.CreateAsync(request));
        }

        // Concurrent updates
        for (int i = 0; i < 50; i++)
        {
            var todoId = initialTodos[i].Id;
            var updateRequest = new UpdateTodoRequest($"Updated Todo {i}", $"Updated Description {i}", DateTime.Today.AddDays(2), 50, false);
            tasks.Add(service.UpdateAsync(todoId, updateRequest));
        }

        // Concurrent reads
        for (int i = 0; i < 100; i++)
        {
            tasks.Add(service.GetAllAsync());
        }

        // Concurrent mark done operations
        for (int i = 50; i < 80; i++)
        {
            var todoId = initialTodos[i].Id;
            tasks.Add(service.MarkDoneAsync(todoId));
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var finalTodos = await service.GetAllAsync();
        finalTodos.Should().HaveCountGreaterThan(250); // Initial 100 + new 200, minus any failed operations

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "Concurrent operations should complete within 10 seconds");

        Console.WriteLine($"Completed 350+ concurrent operations in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task UpdateOperations_PerformanceTest()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange - Create 500 todos to update
        const int todoCount = 500;
        var todos = new List<TodoDto>();
        for (int i = 0; i < todoCount; i++)
        {
            var request = new CreateTodoRequest($"Update Test Todo {i}", $"Description {i}", DateTime.Today.AddDays(1));
            var todo = await service.CreateAsync(request);
            todos.Add(todo);
        }

        var stopwatch = Stopwatch.StartNew();

        // Act - Update all todos
        var updateTasks = new List<Task>();
        for (int i = 0; i < todoCount; i++)
        {
            var updateRequest = new UpdateTodoRequest(
                $"Updated Title {i}",
                $"Updated Description {i}",
                DateTime.Today.AddDays(5),
                75,
                i % 2 == 0
            );
            updateTasks.Add(service.UpdateAsync(todos[i].Id, updateRequest));
        }

        await Task.WhenAll(updateTasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "Updating 500 todos should take less than 3 seconds");

        // Verify updates were applied
        var updatedTodos = await service.GetAllAsync();
        updatedTodos.Should().HaveCount(todoCount);
        updatedTodos.Where(t => t.Title.StartsWith("Updated Title")).Should().HaveCount(todoCount);

        Console.WriteLine($"Updated {todoCount} todos in {stopwatch.ElapsedMilliseconds}ms ({todoCount / (stopwatch.ElapsedMilliseconds / 1000.0):F2} updates/second)");
    }

    [Fact]
    public async Task QueryOperations_PerformanceTest()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange - Create 2000 todos with mixed due dates
        const int todoCount = 2000;
        var todos = new List<Todo>();
        for (int i = 0; i < todoCount; i++)
        {
            todos.Add(new Todo
            {
                Title = $"Query Test Todo {i}",
                Description = $"Description {i}",
                DueDate = DateTime.Today.AddDays(i % 30 - 5), // Mix of past, current, and future dates
                PercentComplete = i % 101,
                IsDone = i % 4 == 0
            });
        }

        context.Todos.AddRange(todos);
        await context.SaveChangesAsync();

        var stopwatch = Stopwatch.StartNew();

        // Act - Perform multiple query operations
        var tasks = new List<Task>();

        // Multiple GetUpcoming calls
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(service.GetUpcomingAsync());
        }

        // Multiple GetById calls
        for (int i = 1; i <= 100; i++)
        {
            tasks.Add(service.GetByIdAsync(i));
        }

        // Multiple GetAll calls
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(service.GetAllAsync());
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(3000, "Query operations should complete within 3 seconds");

        // Verify upcoming query returns reasonable results
        var upcomingTodos = await service.GetUpcomingAsync();
        upcomingTodos.Should().NotBeEmpty("There should be some upcoming todos");

        Console.WriteLine($"Completed 170 query operations on {todoCount} records in {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public async Task DeleteOperations_PerformanceTest()
    {
        using var context = CreateInMemoryContext();
        var service = new TodoService(context);

        // Arrange - Create 300 todos to delete
        const int todoCount = 300;
        var todoIds = new List<int>();
        for (int i = 0; i < todoCount; i++)
        {
            var request = new CreateTodoRequest($"Delete Test Todo {i}", $"Description {i}", DateTime.Today.AddDays(1));
            var todo = await service.CreateAsync(request);
            todoIds.Add(todo.Id);
        }

        var stopwatch = Stopwatch.StartNew();

        // Act - Delete all todos concurrently
        var deleteTasks = todoIds.Select(id => service.DeleteAsync(id)).ToList();
        var results = await Task.WhenAll(deleteTasks);

        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "Deleting 300 todos should take less than 2 seconds");
        results.Should().AllBeOfType<bool>().And.AllSatisfy(result => result.Should().BeTrue());

        // Verify all todos were deleted
        var remainingTodos = await service.GetAllAsync();
        remainingTodos.Should().BeEmpty();

        Console.WriteLine($"Deleted {todoCount} todos in {stopwatch.ElapsedMilliseconds}ms ({todoCount / (stopwatch.ElapsedMilliseconds / 1000.0):F2} deletions/second)");
    }
}