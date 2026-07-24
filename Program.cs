using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WorldCupPredictor.Data;
using WorldCupPredictor.Services;

var builder = WebApplication.CreateBuilder(args);

// Disable reloadOnChange to prevent Linux Docker FileWatcher crash (status 139)
builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration (Supports both PostgreSQL / Supabase and SQLite)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (!string.IsNullOrEmpty(connectionString) && (connectionString.Contains("Host=") || connectionString.Contains("Server=")))
    {
        // Auto-append SSL Mode for Supabase PostgreSQL if not specified
        if (!connectionString.Contains("SSL Mode=", StringComparison.OrdinalIgnoreCase) && 
            !connectionString.Contains("SslMode=", StringComparison.OrdinalIgnoreCase))
        {
            connectionString = connectionString.TrimEnd(';') + ";SSL Mode=Require;Trust Server Certificate=true;";
        }
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
    }
    else
    {
        options.UseSqlite(connectionString ?? "Data Source=worldcup.db");
    }
});

// JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? "WorldCupPredictor2026SuperSecretKey!@#$%^&*()";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "WorldCupPredictor",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "WorldCupPredictor",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Custom Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ScoringService>();

// CORS - allow frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Enable CORS FIRST so error responses always contain CORS headers
app.UseCors("AllowFrontend");

// Global Exception Handler Middleware
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[API Error] {ex}");
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        var errObj = new 
        { 
            message = $"Lỗi hệ thống: {ex.Message}",
            detail = ex.InnerException?.Message ?? ex.Message
        };
        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(errObj));
    }
});

// Auto-migrate database safely
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Enable WAL mode only if SQLite is used
        if (db.Database.ProviderName == "Microsoft.EntityFrameworkCore.Sqlite")
        {
            db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
            db.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
            db.Database.ExecuteSqlRaw("PRAGMA synchronous=NORMAL;");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Database Init Warning] {ex.Message}");
    }
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Serve frontend static files
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Fallback for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
