using MembershipAppBEAPI.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features; // ADD THIS for FormOptions
using Microsoft.Data.SqlClient; // ADD THIS
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using Microsoft.OpenApi.Models;
using Microsoft.IdentityModel.Tokens;
using System.Data; // ADD THIS
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Membership API",
        Version = "v1",
        Description = "API documentation for the MembershipAppBEAPI project."
    });
});

// JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                builder.Configuration["Jwt:Secret"] ?? throw new Exception("JWT Secret is required"))),
            ValidateIssuer = false, // Simplified for now
            ValidateAudience = false, // Simplified for now  
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// File upload limits
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10MB
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", // React dev
                "http://localhost:5173",
                "http://localhost:5027/",
                "https://kind-river-0bb064d10.3.azurestaticapps.net",
                "https://polite-mushroom-0be379810.1.azurestaticapps.net" // Your Azure Static Web App
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();



// Create upload directories
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath))
    Directory.CreateDirectory(uploadsPath);

// Debug endpoint to test database connection
app.MapGet("/debug/members", async (IConfiguration config) =>
{
    try
    {
        await using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        
        var query = "SELECT TOP 5 Id, FullName, Gender FROM Members WHERE IsActive = 1";
        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync();
        
        var members = new List<object>();
        while (await reader.ReadAsync())
        {
            members.Add(new
            {
                Id = reader.GetInt32(0),
                FullName = reader.GetString(1),
                Gender = reader.GetString(2)
            });
        }
        
        return Results.Ok(new { success = true, members = members, count = members.Count });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database Error: {ex.Message}\n\nStack Trace: {ex.StackTrace}");
    }
});

// Health check endpoint
app.MapGet("/health", async (IConfiguration config) =>
{
    try
    {
        await using var connection = new SqlConnection(config.GetConnectionString("DefaultConnection"));
        await connection.OpenAsync();
        return Results.Ok(new { status = "Healthy", database = "Connected", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Unhealthy: {ex.Message}");
    }
});
    
app.MapScalarApiReference(options =>
{
    options.Title = "Membership API Docs";
});

// Middleware pipeline
app.UseCors("AllowFrontend");

//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();