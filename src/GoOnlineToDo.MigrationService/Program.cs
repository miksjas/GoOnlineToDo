using GoOnlineToDo.Infrastructure.Data;
using GoOnlineToDo.MigrationService;
using GoOnlineToDo.ServiceDefaults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<ApiDbInitializer>();

builder.AddServiceDefaults();

builder.Services.AddDbContextPool<ToDoDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("tododb"), sqlOptions =>
    {
        sqlOptions.MigrationsAssembly("GoOnlineToDo.Infrastructure");
    }));

builder.EnrichNpgsqlDbContext<ToDoDbContext>(settings =>
    settings.DisableRetry = true);

var app = builder.Build();

app.Run();