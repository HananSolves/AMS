using System.Text;
using AMS.Application.Helpers;
using AMS.Application.Services;
using AMS.Core.Interfaces;
using AMS.Infrastructure.Data;
using AMS.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// =======================================================
// 🔹 Render-friendly ENV configuration
// =======================================================

// Read connection string from environment variable (for Render deployment)
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") 
    ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Read JWT settings from environment with validation
var jwtSecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? builder.Configuration["JwtSettings:SecretKey"]
    ?? throw new InvalidOperationException("JWT_SECRET_KEY must be configured in environment variables or appsettings.json");

// =======================================================
// Add services to the container
// =======================================================

builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// =======================================================
// Configure Database
// =======================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        }));

// =======================================================
// Configure JWT Settings (ENV-safe)
// =======================================================

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
    options.RequireHttpsMetadata = true; // ✔️ Production-safe
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

    // Read JWT from cookies (MVC support)
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
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

// =======================================================
// Data Protection (for containerized environments)
// =======================================================

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
    .SetApplicationName("AttendanceManagementSystem");

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
        
        // Log connection string info (first 50 chars only for security)
        if (!string.IsNullOrEmpty(connectionString))
        {
            var safeConnectionString = connectionString.Length > 50 
                ? connectionString.Substring(0, 50) + "..." 
                : connectionString;
            logger.LogInformation($"Connection string: {safeConnectionString}");
        }
        else
        {
            logger.LogWarning("Connection string is null or empty!");
        }
        
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Test database connection
        logger.LogInformation("Testing database connection...");
        var canConnect = await context.Database.CanConnectAsync();
        
        if (canConnect)
        {
            logger.LogInformation("Database connection successful");
            await DbInitializer.InitializeAsync(context);
            logger.LogInformation("Database initialization completed successfully");
        }
        else
        {
            logger.LogError("Cannot connect to database");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while initializing the database");
        logger.LogError($"Exception type: {ex.GetType().Name}");
        logger.LogError($"Exception message: {ex.Message}");
        
        if (ex.InnerException != null)
        {
            logger.LogError($"Inner exception: {ex.InnerException.Message}");
        }
        
        // Don't throw - allow app to start even if DB init fails
        // This helps diagnose connection issues in production
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();