using fs_2025_b_api_cosmosdb_20251106.Models;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// Set up configuration to read from appsettings.json and environment variables
var configuration = builder.Configuration;
var cosmosDbEndpoint = configuration["CosmosDb:EndpointUri"];
var cosmosDbKey = configuration["CosmosDb:PrimaryKey"];

var client = new Microsoft.Azure.Cosmos.CosmosClient(cosmosDbEndpoint, cosmosDbKey);
Console.WriteLine("Connected to Cosmos DB");

var database = await client.CreateDatabaseIfNotExistsAsync(configuration["CosmosDb:DatabaseName"]);

var container = await database.Database.CreateContainerIfNotExistsAsync(
    new Microsoft.Azure.Cosmos.ContainerProperties
    {
        Id = configuration["CosmosDb:ContainerName"],
        PartitionKeyPath = "/id"
    });

builder.Services.AddSingleton(container.Container);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/student", () => Results.Ok("ok"));


app.MapGet("/insert", async (Container container) =>
{
    var student = new Student
    {
        id = Guid.NewGuid().ToString(),
        Name = "John Doe",
        Year = 2
    };
    var response = await container.CreateItemAsync(student, new PartitionKey(student.id));
    return Results.Ok(response.Resource);
});

app.Run();


