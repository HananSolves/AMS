using AMS.Application.Common;
using AMS.Application.DTOs.Attendance;

namespace AMS.Application.Services;

public interface IReportService
{
    Task<Result<List<AttendanceReportDto>>> GetStudentAttendanceReportAsync(
        int studentId, int? courseId = null, DateTime? startDate = null, DateTime? endDate = null);
    
    Task<Result<List<AttendanceReportDto>>> GetCourseAttendanceReportAsync(
        int courseId, DateTime? startDate = null, DateTime? endDate = null);
    
    Task<Result<AttendanceReportDto>> GetStudentCourseAttendanceReportAsync(
        int studentId, int courseId, DateTime? startDate = null, DateTime? endDate = null);
}