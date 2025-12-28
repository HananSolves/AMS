using AMS.Application.Common;
using AMS.Application.DTOs.Attendance;

namespace AMS.Application.Services;

public interface IAttendanceService
{
    Task<Result<bool>> MarkAttendanceAsync(MarkAttendanceDto markAttendanceDto, int teacherId);
    Task<Result<List<AttendanceDto>>> GetStudentAttendanceAsync(int studentId, int? courseId = null);
    Task<Result<List<AttendanceDto>>> GetCourseAttendanceAsync(int courseId, DateTime? date = null);
    Task<Result<AttendanceDto>> UpdateAttendanceAsync(int attendanceId, AttendanceDto attendanceDto, int teacherId);
}