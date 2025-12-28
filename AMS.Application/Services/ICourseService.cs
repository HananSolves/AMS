// AMS.Application/Services/ICourseService.cs
using AMS.Application.Common;
using AMS.Application.DTOs.Course;

namespace AMS.Application.Services;

public interface ICourseService
{
    Task<Result<List<CourseDto>>> GetAllCoursesAsync(int? userId = null, string? role = null);
    Task<Result<CourseDto>> GetCourseByIdAsync(int id, int? userId = null);
    Task<Result<CourseDto>> CreateCourseAsync(CreateCourseDto createCourseDto);
    Task<Result<CourseDto>> UpdateCourseAsync(int id, CreateCourseDto updateCourseDto);
    Task<Result<bool>> DeleteCourseAsync(int id);
    Task<Result<List<CourseDto>>> GetTeacherCoursesAsync(int teacherId);
    
    Task<Result<bool>> DeleteCourseAsync(int id, int userId, string role);
}