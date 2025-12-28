using AMS.Application.Helpers;
using AMS.Core.Entities;
using AMS.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(ApplicationDbContext context)
    {
        // Create database if not exists
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Users.AnyAsync())
        {
            return; // Database already seeded
        }

        // Seed default users
        var users = new List<User>
        {
            // Admin user
            new User
            {
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@ams.com",
                PasswordHash = PasswordHelper.HashPassword("Admin@123"),
                Role = UserRole.Admin,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            // Teacher users
            new User
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@uet.edu.pk",
                PasswordHash = PasswordHelper.HashPassword("Teacher@123"),
                Role = UserRole.Teacher,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new User
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@uet.edu.pk",
                PasswordHash = PasswordHelper.HashPassword("Teacher@123"),
                Role = UserRole.Teacher,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            // Student users
            new User
            {
                FirstName = "Ali",
                LastName = "Ahmed",
                Email = "ali.ahmed@uet.edu.pk",
                PasswordHash = PasswordHelper.HashPassword("Student@123"),
                Role = UserRole.Student,
                RegistrationNumber = "2023-CS-001",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new User
            {
                FirstName = "Sara",
                LastName = "Khan",
                Email = "sara.khan@uet.edu.pk",
                PasswordHash = PasswordHelper.HashPassword("Student@123"),
                Role = UserRole.Student,
                RegistrationNumber = "2023-CS-002",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // Get teacher IDs for course creation
        var teacher1 = users.First(u => u.Email == "john.doe@uet.edu.pk");
        var teacher2 = users.First(u => u.Email == "jane.smith@uet.edu.pk");

        // Seed courses
        var courses = new List<Course>
        {
            new Course
            {
                CourseCode = "CSC-414",
                CourseName = "Enterprise Application Development",
                Description = "Learn to develop enterprise-level applications using modern frameworks",
                CreditHours = 3,
                TeacherId = teacher1.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new Course
            {
                CourseCode = "CSC-301",
                CourseName = "Database Systems",
                Description = "Introduction to database design and management",
                CreditHours = 4,
                TeacherId = teacher1.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            },
            new Course
            {
                CourseCode = "CSC-221",
                CourseName = "Data Structures and Algorithms",
                Description = "Fundamental data structures and algorithmic techniques",
                CreditHours = 3,
                TeacherId = teacher2.Id,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            }
        };

        await context.Courses.AddRangeAsync(courses);
        await context.SaveChangesAsync();

        Console.WriteLine("Database initialized with seed data");
    }
}