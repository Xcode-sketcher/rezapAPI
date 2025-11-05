using Scalar.AspNetCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using rezapAPI.Data;
using rezapAPI.Model;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// Configure DbContext with SQL Server
var configured = builder.Configuration.GetConnectionString("DefaultConnection");
var envConn = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING");

// Prefer a non-empty value from configuration, otherwise fall back to the environment variable.
var connectionString = !string.IsNullOrWhiteSpace(configured) ? configured : envConn;

if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Database connection string not configured");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure Identity
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

var configuredJwt = builder.Configuration["JwtSettings:SecretKey"];
var envJwt = Environment.GetEnvironmentVariable("JWT_SECRET_KEY");


var secretKey = !string.IsNullOrWhiteSpace(configuredJwt) ? configuredJwt : envJwt;

if (string.IsNullOrWhiteSpace(secretKey))
    throw new InvalidOperationException("JWT secret key not configured. Set JwtSettings:SecretKey or the JWT_SECRET_KEY environment variable.");

if (secretKey!.Length < 32)
    throw new InvalidOperationException("JWT secret key must be at least 32 characters long for secure signing.");

var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!))
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Apply migrations automatically on startup (for cloud deployments)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Enable CORS
app.UseCors("AllowAll");

// app.UseHttpsRedirection(); // Comentado para facilitar desenvolvimento local

// Enable Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();


app.MapGet("/", () => Results.Ok(new { status = "OK", message = "Rezap API is running" }));

app.Run();
