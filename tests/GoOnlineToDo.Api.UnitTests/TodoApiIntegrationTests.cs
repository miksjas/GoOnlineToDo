using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using GoOnlineToDo.Api;
using GoOnlineToDo.Application.Contracts;
using GoOnlineToDo.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GoOnline.ToDo.Api.UnitTests;

public class TodoApiIntegrationTests : IDisposable
{
    private readonly JsonSerializerOptions _jsonOptions;
    
    public TodoApiIntegrationTests()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    public void Dispose()
    {
    }

    private WebApplicationFactory<IApiMarker> CreateFactory()
    {
        return new WebApplicationFactory<IApiMarker>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ToDoDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    var databaseName = $"TodoApiIntegrationTests_{Guid.NewGuid()}";
                    services.AddDbContext<ToDoDbContext>(options =>
                        options.UseInMemoryDatabase(databaseName));
                });
            });
    }

    [Fact]
    public async Task GetAll_ReturnsEmptyList_WhenNoTodos()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.GetAsync("/todo");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var todos = JsonSerializer.Deserialize<List<TodoDto>>(content, _jsonOptions);
        todos.Should().NotBeNull();
        todos.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.GetAsync("/todo/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_ReturnsBadRequest_WhenIdIsInvalid()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.GetAsync("/todo/0");
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task GetById_ReturnsBadRequest_WhenIdIsNegative()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.GetAsync("/todo/-1");
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task GetUpcoming_ReturnsEmptyList_WhenNoTodos()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.GetAsync("/todo/upcoming");
        
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var todos = JsonSerializer.Deserialize<List<TodoDto>>(content, _jsonOptions);
        todos.Should().NotBeNull();
        todos.Should().BeEmpty();
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenTitleIsEmpty()
    {
        var request = new CreateTodoRequest("", "Description", DateTime.Today.AddDays(1));
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PostAsync("/todo", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Title is required");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenTitleIsTooLong()
    {
        var longTitle = new string('a', 201);
        var request = new CreateTodoRequest(longTitle, "Description", DateTime.Today.AddDays(1));
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PostAsync("/todo", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Title cannot exceed 200 characters");
    }

    [Fact]
    public async Task Create_ReturnsBadRequest_WhenDueDateIsInPast()
    {
        var request = new CreateTodoRequest("Title", "Description", DateTime.Today.AddDays(-1));
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PostAsync("/todo", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Expiry date cannot be in the past");
    }

    [Fact]
    public async Task Create_ReturnsCreated_WhenRequestIsValid()
    {
        var request = new CreateTodoRequest("Valid Title", "Valid Description", DateTime.Today.AddDays(1));
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PostAsync("/todo", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var responseContent = await response.Content.ReadAsStringAsync();
        var todo = JsonSerializer.Deserialize<TodoDto>(responseContent, _jsonOptions);
        todo.Should().NotBeNull();
        todo!.Title.Should().Be("Valid Title");
        todo.Description.Should().Be("Valid Description");
        todo.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenIdIsInvalid()
    {
        var request = new UpdateTodoRequest("Updated Title", "Updated Description", DateTime.Today.AddDays(1), 50, false);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PutAsync("/todo/0", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task Update_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        var request = new UpdateTodoRequest("Updated Title", "Updated Description", DateTime.Today.AddDays(1), 50, false);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PutAsync("/todo/999", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_ReturnsBadRequest_WhenPercentCompleteIsInvalid()
    {
        var request = new UpdateTodoRequest("Updated Title", "Updated Description", DateTime.Today.AddDays(1), 101, false);
        var json = JsonSerializer.Serialize(request, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PutAsync("/todo/1", content);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Percent complete must be between 0 and 100");
    }

    [Fact]
    public async Task UpdatePercent_ReturnsBadRequest_WhenIdIsInvalid()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PatchAsync("/todo/0/percent?percentComplete=75", null);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task UpdatePercent_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PatchAsync("/todo/999/percent?percentComplete=75", null);
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdatePercent_ReturnsBadRequest_WhenPercentIsInvalid()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PatchAsync("/todo/1/percent?percentComplete=150", null);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("Percent complete must be between 0 and 100");
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenIdIsInvalid()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.DeleteAsync("/todo/0");
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_WhenIdIsNegative()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.DeleteAsync("/todo/-1");
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.DeleteAsync("/todo/999");
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MarkDone_ReturnsBadRequest_WhenIdIsInvalid()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PatchAsync("/todo/0/done", null);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task MarkDone_ReturnsBadRequest_WhenIdIsNegative()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PatchAsync("/todo/-1/done", null);
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent.Should().Contain("ID must be greater than 0");
    }

    [Fact]
    public async Task MarkDone_ReturnsNotFound_WhenTodoDoesNotExist()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        
        var response = await client.PatchAsync("/todo/999/done", null);
        
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CompleteWorkflow_CreateGetUpdateDelete_WorksCorrectly()
    {
        using var factory = CreateFactory();
        using var client = factory.CreateClient();

        // Create a todo
        var createRequest = new CreateTodoRequest("Workflow Test", "Test Description", DateTime.Today.AddDays(1));
        var createJson = JsonSerializer.Serialize(createRequest, _jsonOptions);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");

        var createResponse = await client.PostAsync("/todo", createContent);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdTodo = JsonSerializer.Deserialize<TodoDto>(
            await createResponse.Content.ReadAsStringAsync(), _jsonOptions);
        createdTodo.Should().NotBeNull();
        var todoId = createdTodo!.Id;

        // Get the created todo
        var getResponse = await client.GetAsync($"/todo/{todoId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedTodo = JsonSerializer.Deserialize<TodoDto>(
            await getResponse.Content.ReadAsStringAsync(), _jsonOptions);
        retrievedTodo!.Title.Should().Be("Workflow Test");

        // Update the todo
        var updateRequest = new UpdateTodoRequest("Updated Workflow Test", "Updated Description", DateTime.Today.AddDays(2), 75, false);
        var updateJson = JsonSerializer.Serialize(updateRequest, _jsonOptions);
        var updateContent = new StringContent(updateJson, Encoding.UTF8, "application/json");

        var updateResponse = await client.PutAsync($"/todo/{todoId}", updateContent);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Mark as done
        var doneResponse = await client.PatchAsync($"/todo/{todoId}/done", null);
        doneResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Delete the todo
        var deleteResponse = await client.DeleteAsync($"/todo/{todoId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify it's deleted
        var verifyResponse = await client.GetAsync($"/todo/{todoId}");
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}