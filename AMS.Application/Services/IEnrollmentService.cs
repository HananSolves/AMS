using AMS.Application.Common;
using AMS.Application.DTOs.Enrollment;

namespace AMS.Application.Services;

public interface IEnrollmentService
{
    Task<Result<bool>> EnrollStudentAsync(int studentId, int courseId);
    Task<Result<bool>> UnenrollStudentAsync(int studentId, int courseId);
    Task<Result<List<EnrollmentDto>>> GetStudentEnrollmentsAsync(int studentId);
    Task<Result<List<EnrollmentDto>>> GetCourseEnrollmentsAsync(int courseId);
}