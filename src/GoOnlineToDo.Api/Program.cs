using FluentValidation;
using GoOnlineToDo.Api.Endpoints;
using GoOnlineToDo.Api.Validators;
using GoOnlineToDo.Application.Contracts;
using GoOnlineToDo.Application.Interfaces;
using GoOnlineToDo.Infrastructure.Data;
using GoOnlineToDo.Infrastructure.Services;
using GoOnlineToDo.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsEnvironment("Testing"))
{
    var connectionString = builder.Configuration.GetConnectionString("tododb");
    builder.Services.AddDbContext<ToDoDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddScoped<ITodoService, TodoService>();

builder.Services.AddScoped<IValidator<CreateTodoRequest>, CreateTodoRequestValidator>();
//builder.Services.AddScoped<IValidator<UpdateTodoRequest>, UpdateTodoRequestValidator>();
builder.Services.AddScoped<IValidator<UpdateTodoWithIdRequest>, UpdateTodoWithIdRequestValidator>();
builder.Services.AddScoped<IValidator<UpdatePercentRequest>, UpdatePercentRequestValidator>();
builder.Services.AddScoped<IValidator<IdRequest>, IdValidator>();

//aspire and OLTP monitoring setup
builder.AddServiceDefaults();

builder.Services.AddProblemDetails();

builder.Services.AddOpenApi();

var app = builder.Build();

// Add Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "GoOnline ToDo API Reference";
        options.Favicon = "path";
        options.Servers = [];
        options.Theme = ScalarTheme.BluePlanet;
        options.DefaultHttpClient = new KeyValuePair<ScalarTarget, ScalarClient>(ScalarTarget.CSharp, ScalarClient.RestSharp);
    });
}

app.UseHttpsRedirection();
app.UseTodoEndpoints();

app.Run();