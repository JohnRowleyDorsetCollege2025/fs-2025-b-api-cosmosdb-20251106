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


app.MapGet("/students/{id}", async (string id, Container container) =>
{
    try
    {
        var response = await container.ReadItemAsync<Student>(id, new PartitionKey(id));
        return Results.Ok(response.Resource);
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
        return Results.NotFound();
    }
});

// New endpoint: return all students
app.MapGet("/students", async (Container container) =>
{
    try
    {
        var query = new QueryDefinition("SELECT * FROM c");
        var iterator = container.GetItemQueryIterator<Student>(query);
        var students = new List<Student>();

        while (iterator.HasMoreResults)
        {
            var response = await iterator.ReadNextAsync();
            students.AddRange(response.Resource);
        }

        return Results.Ok(students);
    }
    catch (CosmosException ex)
    {
        // return the Cosmos status code and message for easier debugging
        return Results.StatusCode((int)ex.StatusCode);
    }
});

// POST endpoint: create a student (id auto-generated). Input JSON: { "name": "...", "year": 1 }
app.MapPost("/students", async (CreateStudentRequest request, Container container) =>
{
    if (string.IsNullOrWhiteSpace(request.Name) || request.Year <= 0)
    {
        return Results.BadRequest("Name must be provided and Year must be > 0.");
    }

    var student = new Student
    {
        id = Guid.NewGuid().ToString(),
        Name = request.Name,
        Year = request.Year
    };

    try
    {
        var response = await container.CreateItemAsync(student, new PartitionKey(student.id));
        // Return 201 Created with Location header
        return Results.Created($"/students/{student.id}", response.Resource);
    }
    catch (CosmosException ex)
    {
        return Results.StatusCode((int)ex.StatusCode);
    }
});

app.Run();

// DTO used for binding the POST body
file record CreateStudentRequest(string Name, int Year);


