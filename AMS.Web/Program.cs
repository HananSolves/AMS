using System.Text;
using AMS.Application.Helpers;
using AMS.Application.Services;
using AMS.Core.Interfaces;
using AMS.Infrastructure.Data;
using AMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 🔹 Database Connection Helper
// =======================================================

string GetConnectionString()
{
    // Try DATABASE_URL from Render (PostgreSQL format)
    var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
    if (!string.IsNullOrEmpty(databaseUrl))
    {
        Console.WriteLine("Using DATABASE_URL from Render");
        try
        {
            // Parse PostgreSQL connection string
            // Format: postgresql://username:password@host:port/database
            var uri = new Uri(databaseUrl);
            var host = uri.Host;
            var port = uri.Port > 0 ? uri.Port : 5432;
            var database = uri.AbsolutePath.TrimStart('/');
            var userInfo = uri.UserInfo.Split(':');
            var username = userInfo[0];
            var password = userInfo.Length > 1 ? userInfo[1] : "";

            var connectionString = $"Host={host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
            Console.WriteLine($"Parsed connection: Host={host};Port={port};Database={database};Username={username}");
            return connectionString;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing DATABASE_URL: {ex.Message}");
            throw;
        }
    }

    // Fallback to appsettings.json (for local development)
    Console.WriteLine("Using connection string from appsettings.json");
    return builder.Configuration.GetConnectionString("DefaultConnection")!;
}

// =======================================================
// Add services to the container
// =======================================================

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// =======================================================
// Configure Database (PostgreSQL)
// =======================================================

var connectionString = GetConnectionString();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorCodesToAdd: null);
        npgsqlOptions.CommandTimeout(60);
    }));

// =======================================================
// Configure JWT Settings
// =======================================================

var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT_SECRET_KEY must be configured");

builder.Services.Configure<JwtSettings>(options =>
{
    options.SecretKey = jwtSecretKey;
    options.Issuer = builder.Configuration["JwtSettings:Issuer"]!;
    options.Audience = builder.Configuration["JwtSettings:Audience"]!;
    options.AccessTokenExpirationMinutes = int.Parse(builder.Configuration["JwtSettings:AccessTokenExpirationMinutes"]!);
    options.RefreshTokenExpirationDays = int.Parse(builder.Configuration["JwtSettings:RefreshTokenExpirationDays"]!);
});

var key = Encoding.UTF8.GetBytes(jwtSecretKey);

// =======================================================
// Configure Authentication
// =======================================================

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false; // Set to true in production if using HTTPS
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["JwtSettings:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Cookies["AccessToken"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// =======================================================
// Session
// =======================================================

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.None; // Allow non-HTTPS for Render's internal routing
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// =======================================================
// Data Protection (Store keys in database for container persistence)
// =======================================================

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<ApplicationDbContext>()
    .SetApplicationName("AttendanceManagementSystem")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// =======================================================
// Dependency Injection
// =======================================================

// Repositories & Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICourseService, CourseService>();
builder.Services.AddScoped<IAttendanceService, AttendanceService>();
builder.Services.AddScoped<IEnrollmentService, EnrollmentService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IPdfService, PdfService>();

// Helpers
builder.Services.AddScoped<JwtHelper>();

builder.Services.AddHttpContextAccessor();

// =======================================================
// CORS
// =======================================================

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

// =======================================================
// Database Initialization
// =======================================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Starting database initialization...");
        Console.WriteLine("Initializing database...");
        
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Test database connection
        logger.LogInformation("Testing database connection...");
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("Database connection successful");
            Console.WriteLine("Database connected successfully!");
            
            // Run migrations
            logger.LogInformation("Running migrations...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Migrations completed");
            
            // Initialize seed data
            await DbInitializer.InitializeAsync(context);
            logger.LogInformation("Database initialization completed successfully");
            Console.WriteLine("Database initialized successfully!");
        }
        else
        {
            logger.LogError("Cannot connect to database");
            Console.WriteLine("ERROR: Cannot connect to database");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        Console.WriteLine($"Database initialization error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
        
        if (ex.InnerException != null)
        {
            Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
        }
    }
}

// =======================================================
// HTTP Pipeline
// =======================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseStatusCodePagesWithReExecute("/Error/{0}");

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

// Health check endpoint
app.MapGet("/health", async (ApplicationDbContext dbContext) => 
{
    try
    {
        var canConnect = await dbContext.Database.CanConnectAsync();
        return Results.Ok(new { 
            status = canConnect ? "healthy" : "unhealthy",
            database = canConnect ? "connected" : "disconnected",
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName 
        });
    }
    catch (Exception ex)
    {
        return Results.Ok(new { 
            status = "unhealthy",
            database = "error",
            error = ex.Message,
            timestamp = DateTime.UtcNow 
        });
    }
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

Console.WriteLine("Application starting...");
app.Run();