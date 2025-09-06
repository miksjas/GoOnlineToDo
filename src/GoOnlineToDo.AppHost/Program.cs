var builder = DistributedApplication.CreateBuilder(args);

// Postgres container
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume(isReadOnly: false)
    .WithPgAdmin()
    .WithLifetime(ContainerLifetime.Persistent);

// Add logical database inside postgres
var db = postgres.AddDatabase("tododb");

// Migration service depends on DB being ready
var migrationService = builder.AddProject<Projects.GoOnlineToDo_MigrationService>("migration")
    .WithReference(db)
    .WaitFor(db);

//Scalar UI
var api = builder.AddProject<Projects.GoOnlineToDo_Api>("goonlinetodo-api")
    .WithReference(db)
    .WaitFor(db)
    .WithUrlForEndpoint("https", url =>
    {
        url.DisplayText = "Scalar (HTTPS)";
        url.Url = "/scalar";
    });

builder.Build().Run();