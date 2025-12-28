using AMS.Application.Common;
using AMS.Application.DTOs.Enrollment;
using AMS.Core.Entities;
using AMS.Core.Enums;
using AMS.Core.Interfaces;

namespace AMS.Application.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IUnitOfWork _unitOfWork;

    public EnrollmentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> EnrollStudentAsync(int studentId, int courseId)
    {
        try
        {
            // Verify student exists and has student role
            var userRepo = _unitOfWork.Repository<User>();
            var student = await userRepo.GetByIdAsync(studentId);

            if (student == null)
            {
                return Result<bool>.FailureResult("Student not found");
            }

            if (student.Role != UserRole.Student)
            {
                return Result<bool>.FailureResult("User is not a student");
            }

            // Verify course exists
            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(courseId);

            if (course == null)
            {
                return Result<bool>.FailureResult("Course not found");
            }

            if (!course.IsActive)
            {
                return Result<bool>.FailureResult("Course is not active");
            }

            // Check if already enrolled
            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var existingEnrollment = await enrollmentRepo.FirstOrDefaultAsync(e =>
                e.StudentId == studentId && e.CourseId == courseId && e.IsActive);

            if (existingEnrollment != null)
            {
                return Result<bool>.FailureResult("Student is already enrolled in this course");
            }

            var enrollment = new Enrollment
            {
                StudentId = studentId,
                CourseId = courseId,
                EnrolledAt = DateTime.UtcNow,
                IsActive = true
            };

            await enrollmentRepo.AddAsync(enrollment);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.SuccessResult(true, "Successfully enrolled in course");
        }
        catch (Exception ex)
        {
            return Result<bool>.FailureResult(
                $"An error occurred during enrollment: {ex.Message}");
        }
    }

    public async Task<Result<bool>> UnenrollStudentAsync(int studentId, int courseId)
    {
        try
        {
            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var enrollment = await enrollmentRepo.FirstOrDefaultAsync(e =>
                e.StudentId == studentId && e.CourseId == courseId && e.IsActive);

            if (enrollment == null)
            {
                return Result<bool>.FailureResult("Enrollment not found");
            }

            // Soft delete
            enrollment.IsActive = false;
            enrollmentRepo.Update(enrollment);
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.SuccessResult(true, "Successfully unenrolled from course");
        }
        catch (Exception ex)
        {
            return Result<bool>.FailureResult(
                $"An error occurred during unenrollment: {ex.Message}");
        }
    }

    public async Task<Result<List<EnrollmentDto>>> GetStudentEnrollmentsAsync(int studentId)
    {
        try
        {
            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var enrollments = await enrollmentRepo.FindAsync(e =>
                e.StudentId == studentId && e.IsActive);

            var courseRepo = _unitOfWork.Repository<Course>();
            var enrollmentDtos = new List<EnrollmentDto>();

            foreach (var enrollment in enrollments)
            {
                var course = await courseRepo.GetByIdAsync(enrollment.CourseId);
                if (course != null)
                {
                    enrollmentDtos.Add(new EnrollmentDto
                    {
                        CourseId = course.Id,
                        CourseName = course.CourseName,
                        CourseCode = course.CourseCode,
                        EnrolledAt = enrollment.EnrolledAt
                    });
                }
            }

            return Result<List<EnrollmentDto>>.SuccessResult(
                enrollmentDtos.OrderBy(e => e.CourseCode).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<EnrollmentDto>>.FailureResult(
                $"An error occurred while retrieving enrollments: {ex.Message}");
        }
    }
    
    public async Task<Result<List<EnrollmentDto>>> GetCourseEnrollmentsAsync(int courseId)
    {
        try
        {
            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var enrollments = await enrollmentRepo.FindAsync(e =>
                e.CourseId == courseId && e.IsActive);

            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(courseId);

            if (course == null)
            {
                return Result<List<EnrollmentDto>>.FailureResult("Course not found");
            }

            var userRepo = _unitOfWork.Repository<User>();
            var enrollmentDtos = new List<EnrollmentDto>();

            foreach (var enrollment in enrollments)
            {
                var student = await userRepo.GetByIdAsync(enrollment.StudentId);
                if (student != null)
                {
                    enrollmentDtos.Add(new EnrollmentDto
                    {
                        CourseId = course.Id,
                        CourseName = course.CourseName,
                        CourseCode = course.CourseCode,
                        EnrolledAt = enrollment.EnrolledAt,
                        StudentId = student.Id,
                        StudentName = $"{student.FirstName} {student.LastName}",
                        RegistrationNumber = student.RegistrationNumber ?? "N/A"
                    });
                }
            }

            return Result<List<EnrollmentDto>>.SuccessResult(enrollmentDtos);
        }
        catch (Exception ex)
        {
            return Result<List<EnrollmentDto>>.FailureResult(
                $"An error occurred while retrieving course enrollments: {ex.Message}");
        }
    }    
}