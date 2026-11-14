
using DAL;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Database connection string
string dbConnectionString =
    (builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found."))
    + "Database=RoniRoseDB;";

// Register UserDB
builder.Services.AddSingleton<DAL.UserDB>(_ =>
    new DAL.UserDB(dbConnectionString));

// Register OrderDB
builder.Services.AddScoped<DAL.OrderDB>(_ =>
    new DAL.OrderDB(dbConnectionString));

// Register ProductDB
builder.Services.AddScoped<ProductDB>(_ =>
    new ProductDB(dbConnectionString));

var app = builder.Build();

// Server connection string for database initialization
string serverConnectionString =
    builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Initialize database
await DAL.DatabaseInitializer.InitializeAsync(serverConnectionString);

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
