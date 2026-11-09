using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;
using RaceDay.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Swagger: builds the browser page that lists and tests every endpoint.
// Reads the XML documentation file produced by the build so every action's
// summary and response types show up in the UI.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay race event management system. " +
                      "Supports two roles: Organiser (manages events, categories, results) " +
                      "and Participant (browses events, enrols, views own results). " +
                      "Authentication uses a server-side session cookie."
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Password hashing service
builder.Services.AddScoped<IPasswordService, PasswordService>();

// Azure Blob Storage for banner and profile images (only the API talks to Azure)
builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Session support: the server remembers who is logged in between requests
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// Create or update the database when the API starts. A brand-new database (for example the
// one inside Docker) therefore gets its tables and seed data without any manual step.
// The in-memory database used by the tests is not relational, so it is skipped.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RaceDayDbContext>();

    if (db.Database.IsRelational())
    {
        // SQL Server can take a few seconds to accept connections after it starts, so retry
        var attempts = 0;
        while (true)
        {
            try
            {
                db.Database.Migrate();
                break;
            }
            catch (Exception ex) when (attempts < 10)
            {
                attempts++;
                app.Logger.LogWarning(ex, "The database is not ready yet (attempt {Attempt} of 10). Retrying...", attempts);
                Thread.Sleep(TimeSpan.FromSeconds(3));
            }
        }
    }
}

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    options.DocumentTitle = "RaceDay API";
});

app.UseHttpsRedirection();
app.UseSession();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Lets the test project start the API in memory
public partial class Program { }