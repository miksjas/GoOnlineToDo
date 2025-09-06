using FluentValidation;
using GoOnlineToDo.Api.Extensions;
using GoOnlineToDo.Api.Validators;
using GoOnlineToDo.Application.Contracts;
using GoOnlineToDo.Application.Interfaces;

namespace GoOnlineToDo.Api.Endpoints;

public static class TodoEndpoints
{
    public static IEndpointRouteBuilder UseTodoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app
            .MapGroup("/todo")
            .WithTags("GoOnlineToDo")
            .WithDescription("Endpoints for GoOnline ToDo api")
            .WithOpenApi();

        group.MapGet("", static async (
              ITodoService todoService,
              CancellationToken ct) =>
        {
            return TypedResults.Ok(await todoService.GetAllAsync());
        })
        .WithName("getAll")
        .Produces<List<TodoDto>>(StatusCodes.Status200OK);

        group.MapGet("{id:int}", static async (
          int id,
          ITodoService todoService,
          IValidator<IdRequest> validator,
          CancellationToken ct) =>
        {
            var request = new IdRequest(id);
            return await request.ValidateAsync(validator, async () =>
            {
                var todo = await todoService.GetByIdAsync(id);
                if (todo is null)
                {
                    return Results.NotFound();
                }
                return TypedResults.Ok(todo);
            });
        })
        .WithName("getById")
        .Produces<TodoDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        group.MapGet("upcoming", static async (
          ITodoService todoService,
          CancellationToken ct) =>
        {
            return TypedResults.Ok(await todoService.GetUpcomingAsync());
        })
        .WithName("getUpcoming")
        .Produces<List<TodoDto>>(StatusCodes.Status200OK);

        group.MapPost("", static async (
          CreateTodoRequest request,
          ITodoService todoService,
          IValidator<CreateTodoRequest> validator,
          CancellationToken ct) =>
        {
            return await request.ValidateAsync(validator, async () =>
            {
                var todo = await todoService.CreateAsync(request);
                return Results.CreatedAtRoute("Create", new { id = todo.Id }, todo);
            });
        })
        .WithName("create")
        .Produces(StatusCodes.Status201Created)
        .ProducesValidationProblem();

        group.MapPut("{id:int}", static async (
          int id,
          UpdateTodoRequest updateRequest,
          ITodoService todoService,
          IValidator<UpdateTodoWithIdRequest> validator,
          CancellationToken ct) =>
        {
            var request = new UpdateTodoWithIdRequest(id, updateRequest.Title, updateRequest.Description, updateRequest.DueDate, updateRequest.PercentComplete, updateRequest.IsDone);
            return await request.ValidateAsync(validator, async () =>
            {
                var todo = await todoService.UpdateAsync(id, updateRequest);
                if (todo is null)
                {
                    return Results.NotFound();
                }
                return TypedResults.Ok(todo);
            });
        })
        .WithName("update")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        group.MapPatch("{id:int}/percent", static async (
          int id,
          int percentComplete,
          ITodoService todoService,
          IValidator<UpdatePercentRequest> validator,
          CancellationToken ct) =>
         {
             var request = new UpdatePercentRequest(id, percentComplete);
             return await request.ValidateAsync(validator, async () =>
             {
                 var todo = await todoService.UpdatePercentAsync(id, percentComplete);
                 if (todo is null)
                 {
                     return Results.NotFound();
                 }
                 return TypedResults.Ok(todo);
             });
         })
        .WithName("updatePercent")
        .Produces(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesValidationProblem();

        group.MapDelete("{id:int}", static async (
           int id,
           ITodoService todoService,
           IValidator<IdRequest> validator,
           CancellationToken ct) =>
         {
             var request = new IdRequest(id);
             return await request.ValidateAsync(validator, async () =>
             {
                 var deleted = await todoService.DeleteAsync(id);
                 if (!deleted)
                 {
                     return Results.NotFound();
                 }
                 return Results.NoContent();
             });
         })
         .WithName("delete")
         .Produces(StatusCodes.Status400BadRequest)
         .Produces(StatusCodes.Status404NotFound)
         .Produces(StatusCodes.Status204NoContent)
         .ProducesValidationProblem();

        group.MapPatch("{id:int}/done", static async (
          int id,
          ITodoService todoService,
          IValidator<IdRequest> validator,
          CancellationToken ct) =>
        {
            var request = new IdRequest(id);
            return await request.ValidateAsync(validator, async () =>
            {
                var todo = await todoService.MarkDoneAsync(id);
                if (todo == null)
                {
                    return Results.NotFound();
                }
                return Results.Ok(todo);
            });
        })
        .WithName("markDone")
        .Produces(StatusCodes.Status400BadRequest)
        .Produces(StatusCodes.Status404NotFound)
        .Produces(StatusCodes.Status200OK)
        .ProducesValidationProblem();

        return app;
    }
}