using Microsoft.EntityFrameworkCore;
using TestTask.Core.Exceptions;
using TestTask.Core.Interfaces;
using TestTask.Infrastructure;
using TestTask.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// ----- Services -----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "TestTask API",
        Version = "v1",
        Description = "WebAPI for uploading, aggregating and querying timescale CSV results."
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ICsvProcessingService, CsvProcessingService>();
builder.Services.AddScoped<IResultsService, ResultsService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    const int maxAttempts = 10;
    for (var attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            db.Database.EnsureCreated();
            break;
        }
        catch (Exception ex) when (attempt < maxAttempts)
        {
            var delay = TimeSpan.FromSeconds(Math.Min(2 * attempt, 15));
            logger.LogWarning(ex,
                "Database not ready yet (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay}s...",
                attempt, maxAttempts, delay.TotalSeconds);
            Thread.Sleep(delay);
        }
    }
}

// ----- Middleware -----
app.UseSwagger();
app.UseSwaggerUI();

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (CsvValidationException ex)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { errors = ex.Errors });
    }
    catch (Exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
    }
});

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }