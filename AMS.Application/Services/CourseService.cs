// AMS.Application/Services/CourseService.cs
using AMS.Application.Common;
using AMS.Application.DTOs.Course;
using AMS.Core.Entities;
using AMS.Core.Enums;
using AMS.Core.Interfaces;

namespace AMS.Application.Services;

public class CourseService : ICourseService
{
    private readonly IUnitOfWork _unitOfWork;

    public CourseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<CourseDto>>> GetAllCoursesAsync(int? userId = null, string? role = null)
    {
        try
        {
            var courseRepo = _unitOfWork.Repository<Course>();
            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var userRepo = _unitOfWork.Repository<User>();

            var courses = await courseRepo.FindAsync(c => c.IsActive);
            var courseDtos = new List<CourseDto>();

            foreach (var course in courses)
            {
                var teacher = await userRepo.GetByIdAsync(course.TeacherId);
                var enrollments = await enrollmentRepo.FindAsync(e => 
                    e.CourseId == course.Id && e.IsActive);

                bool isEnrolled = false;
                if (userId.HasValue && role == "Student")
                {
                    isEnrolled = enrollments.Any(e => e.StudentId == userId.Value);
                }

                courseDtos.Add(new CourseDto
                {
                    Id = course.Id,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Description = course.Description,
                    CreditHours = course.CreditHours,
                    TeacherId = course.TeacherId,
                    TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Unknown",
                    EnrolledStudents = enrollments.Count(),
                    IsEnrolled = isEnrolled,
                    CreatedAt = course.CreatedAt
                });
            }

            return Result<List<CourseDto>>.SuccessResult(
                courseDtos.OrderBy(c => c.CourseCode).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<CourseDto>>.FailureResult(
                $"An error occurred while retrieving courses: {ex.Message}");
        }
    }

    public async Task<Result<CourseDto>> GetCourseByIdAsync(int id, int? userId = null)
    {
        try
        {
            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(id);

            if (course == null)
            {
                return Result<CourseDto>.FailureResult("Course not found");
            }

            var userRepo = _unitOfWork.Repository<User>();
            var teacher = await userRepo.GetByIdAsync(course.TeacherId);

            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var enrollments = await enrollmentRepo.FindAsync(e => 
                e.CourseId == course.Id && e.IsActive);

            bool isEnrolled = false;
            if (userId.HasValue)
            {
                isEnrolled = enrollments.Any(e => e.StudentId == userId.Value);
            }

            var courseDto = new CourseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Description = course.Description,
                CreditHours = course.CreditHours,
                TeacherId = course.TeacherId,
                TeacherName = teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : "Unknown",
                EnrolledStudents = enrollments.Count(),
                IsEnrolled = isEnrolled,
                CreatedAt = course.CreatedAt
            };

            return Result<CourseDto>.SuccessResult(courseDto);
        }
        catch (Exception ex)
        {
            return Result<CourseDto>.FailureResult(
                $"An error occurred while retrieving course: {ex.Message}");
        }
    }

    public async Task<Result<CourseDto>> CreateCourseAsync(CreateCourseDto createCourseDto)
    {
        try
        {
            var courseRepo = _unitOfWork.Repository<Course>();

            // Check if course code already exists
            var existingCourse = await courseRepo.FirstOrDefaultAsync(c => 
                c.CourseCode.ToLower() == createCourseDto.CourseCode.ToLower());

            if (existingCourse != null)
            {
                return Result<CourseDto>.FailureResult("Course code already exists");
            }

            // Verify teacher exists and has teacher role
            var userRepo = _unitOfWork.Repository<User>();
            var teacher = await userRepo.GetByIdAsync(createCourseDto.TeacherId);

            if (teacher == null)
            {
                return Result<CourseDto>.FailureResult("Teacher not found");
            }

            if (teacher.Role != UserRole.Teacher)
            {
                return Result<CourseDto>.FailureResult("Selected user is not a teacher");
            }

            var course = new Course
            {
                CourseCode = createCourseDto.CourseCode.ToUpper().Trim(),
                CourseName = createCourseDto.CourseName.Trim(),
                Description = createCourseDto.Description.Trim(),
                CreditHours = createCourseDto.CreditHours,
                TeacherId = createCourseDto.TeacherId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await courseRepo.AddAsync(course);
            await _unitOfWork.SaveChangesAsync();

            var courseDto = new CourseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Description = course.Description,
                CreditHours = course.CreditHours,
                TeacherId = course.TeacherId,
                TeacherName = $"{teacher.FirstName} {teacher.LastName}",
                EnrolledStudents = 0,
                CreatedAt = course.CreatedAt
            };

            return Result<CourseDto>.SuccessResult(courseDto, "Course created successfully");
        }
        catch (Exception ex)
        {
            return Result<CourseDto>.FailureResult(
                $"An error occurred while creating course: {ex.Message}");
        }
    }

    public async Task<Result<CourseDto>> UpdateCourseAsync(int id, CreateCourseDto updateCourseDto)
    {
        try
        {
            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(id);

            if (course == null)
            {
                return Result<CourseDto>.FailureResult("Course not found");
            }

            // Check if course code is being changed and if new code already exists
            if (course.CourseCode.ToLower() != updateCourseDto.CourseCode.ToLower())
            {
                var existingCourse = await courseRepo.FirstOrDefaultAsync(c => 
                    c.CourseCode.ToLower() == updateCourseDto.CourseCode.ToLower() && c.Id != id);

                if (existingCourse != null)
                {
                    return Result<CourseDto>.FailureResult("Course code already exists");
                }
            }

            // Verify teacher exists and has teacher role
            var userRepo = _unitOfWork.Repository<User>();
            var teacher = await userRepo.GetByIdAsync(updateCourseDto.TeacherId);

            if (teacher == null)
            {
                return Result<CourseDto>.FailureResult("Teacher not found");
            }

            if (teacher.Role != UserRole.Teacher)
            {
                return Result<CourseDto>.FailureResult("Selected user is not a teacher");
            }

            course.CourseCode = updateCourseDto.CourseCode.ToUpper().Trim();
            course.CourseName = updateCourseDto.CourseName.Trim();
            course.Description = updateCourseDto.Description.Trim();
            course.CreditHours = updateCourseDto.CreditHours;
            course.TeacherId = updateCourseDto.TeacherId;

            courseRepo.Update(course);
            await _unitOfWork.SaveChangesAsync();

            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var enrollments = await enrollmentRepo.FindAsync(e => 
                e.CourseId == course.Id && e.IsActive);

            var courseDto = new CourseDto
            {
                Id = course.Id,
                CourseCode = course.CourseCode,
                CourseName = course.CourseName,
                Description = course.Description,
                CreditHours = course.CreditHours,
                TeacherId = course.TeacherId,
                TeacherName = $"{teacher.FirstName} {teacher.LastName}",
                EnrolledStudents = enrollments.Count(),
                CreatedAt = course.CreatedAt
            };

            return Result<CourseDto>.SuccessResult(courseDto, "Course updated successfully");
        }
        catch (Exception ex)
        {
            return Result<CourseDto>.FailureResult(
                $"An error occurred while updating course: {ex.Message}");
        }
    }

    public async Task<Result<bool>> DeleteCourseAsync(int id)
    {
        try
        {
            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(id);

            if (course == null)
            {
                return Result<bool>.FailureResult("Course not found");
            }

            // Soft delete
            course.IsActive = false;
            courseRepo.Update(course);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.SuccessResult(true, "Course deleted successfully");
        }
        catch (Exception ex)
        {
            return Result<bool>.FailureResult(
                $"An error occurred while deleting course: {ex.Message}");
        }
    }

    public async Task<Result<List<CourseDto>>> GetTeacherCoursesAsync(int teacherId)
    {
        try
        {
            var courseRepo = _unitOfWork.Repository<Course>();
            var courses = await courseRepo.FindAsync(c => 
                c.TeacherId == teacherId && c.IsActive);

            var userRepo = _unitOfWork.Repository<User>();
            var teacher = await userRepo.GetByIdAsync(teacherId);

            if (teacher == null)
            {
                return Result<List<CourseDto>>.FailureResult("Teacher not found");
            }

            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var courseDtos = new List<CourseDto>();

            foreach (var course in courses)
            {
                var enrollments = await enrollmentRepo.FindAsync(e => 
                    e.CourseId == course.Id && e.IsActive);

                courseDtos.Add(new CourseDto
                {
                    Id = course.Id,
                    CourseCode = course.CourseCode,
                    CourseName = course.CourseName,
                    Description = course.Description,
                    CreditHours = course.CreditHours,
                    TeacherId = course.TeacherId,
                    TeacherName = $"{teacher.FirstName} {teacher.LastName}",
                    EnrolledStudents = enrollments.Count(),
                    CreatedAt = course.CreatedAt
                });
            }

            return Result<List<CourseDto>>.SuccessResult(
                courseDtos.OrderBy(c => c.CourseCode).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<CourseDto>>.FailureResult(
                $"An error occurred while retrieving teacher courses: {ex.Message}");
        }
    }
    
    public async Task<Result<bool>> DeleteCourseAsync(int id, int userId, string role)
    {
        try
        {
            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(id);

            if (course == null)
            {
                return Result<bool>.FailureResult("Course not found");
            }

            // Check authorization
            if (role == "Teacher" && course.TeacherId != userId)
            {
                return Result<bool>.FailureResult("You can only delete courses that you created");
            }

            // Soft delete
            course.IsActive = false;
            courseRepo.Update(course);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.SuccessResult(true, "Course deleted successfully");
        }
        catch (Exception ex)
        {
            return Result<bool>.FailureResult(
                $"An error occurred while deleting course: {ex.Message}");
        }
    }
}