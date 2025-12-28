using AMS.Application.Common;
using AMS.Application.DTOs.Attendance;
using AMS.Core.Entities;
using AMS.Core.Enums;
using AMS.Core.Interfaces;

namespace AMS.Application.Services;

public class ReportService : IReportService
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<List<AttendanceReportDto>>> GetStudentAttendanceReportAsync(
        int studentId, int? courseId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var attendanceRepo = _unitOfWork.Repository<Attendance>();
            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var courseRepo = _unitOfWork.Repository<Course>();
            var userRepo = _unitOfWork.Repository<User>();

            var student = await userRepo.GetByIdAsync(studentId);
            if (student == null)
            {
                return Result<List<AttendanceReportDto>>.FailureResult("Student not found");
            }

            // Get student's enrolled courses
            var enrollments = await enrollmentRepo.FindAsync(e =>
                e.StudentId == studentId && e.IsActive);

            if (courseId.HasValue)
            {
                enrollments = enrollments.Where(e => e.CourseId == courseId.Value);
            }

            var reports = new List<AttendanceReportDto>();

            foreach (var enrollment in enrollments)
            {
                var course = await courseRepo.GetByIdAsync(enrollment.CourseId);
                if (course == null) continue;

                var attendances = await attendanceRepo.FindAsync(a =>
                    a.StudentId == studentId && a.CourseId == enrollment.CourseId);

                // Apply date filters
                if (startDate.HasValue)
                {
                    attendances = attendances.Where(a => a.Date >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    attendances = attendances.Where(a => a.Date <= endDate.Value);
                }

                var attendanceList = attendances.ToList();
                var totalClasses = attendanceList.Count;
                var presentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Present);
                var absentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Absent);
                var lateCount = attendanceList.Count(a => a.Status == AttendanceStatus.Late);

                var percentage = totalClasses > 0
                    ? Math.Round((decimal)(presentCount + lateCount) / totalClasses * 100, 2)
                    : 0;

                reports.Add(new AttendanceReportDto
                {
                    StudentName = $"{student.FirstName} {student.LastName}",
                    RegistrationNumber = student.RegistrationNumber ?? "N/A",
                    CourseName = course.CourseName,
                    TotalClasses = totalClasses,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LateCount = lateCount,
                    AttendancePercentage = percentage
                });
            }

            return Result<List<AttendanceReportDto>>.SuccessResult(
                reports.OrderBy(r => r.CourseName).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<AttendanceReportDto>>.FailureResult(
                $"An error occurred while generating report: {ex.Message}");
        }
    }

    public async Task<Result<List<AttendanceReportDto>>> GetCourseAttendanceReportAsync(
        int courseId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var attendanceRepo = _unitOfWork.Repository<Attendance>();
            var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
            var courseRepo = _unitOfWork.Repository<Course>();
            var userRepo = _unitOfWork.Repository<User>();

            var course = await courseRepo.GetByIdAsync(courseId);
            if (course == null)
            {
                return Result<List<AttendanceReportDto>>.FailureResult("Course not found");
            }

            // Get all enrolled students
            var enrollments = await enrollmentRepo.FindAsync(e =>
                e.CourseId == courseId && e.IsActive);

            var reports = new List<AttendanceReportDto>();

            foreach (var enrollment in enrollments)
            {
                var student = await userRepo.GetByIdAsync(enrollment.StudentId);
                if (student == null) continue;

                var attendances = await attendanceRepo.FindAsync(a =>
                    a.StudentId == enrollment.StudentId && a.CourseId == courseId);

                // Apply date filters
                if (startDate.HasValue)
                {
                    attendances = attendances.Where(a => a.Date >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    attendances = attendances.Where(a => a.Date <= endDate.Value);
                }

                var attendanceList = attendances.ToList();
                var totalClasses = attendanceList.Count;
                var presentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Present);
                var absentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Absent);
                var lateCount = attendanceList.Count(a => a.Status == AttendanceStatus.Late);

                var percentage = totalClasses > 0
                    ? Math.Round((decimal)(presentCount + lateCount) / totalClasses * 100, 2)
                    : 0;

                reports.Add(new AttendanceReportDto
                {
                    StudentName = $"{student.FirstName} {student.LastName}",
                    RegistrationNumber = student.RegistrationNumber ?? "N/A",
                    CourseName = course.CourseName,
                    TotalClasses = totalClasses,
                    PresentCount = presentCount,
                    AbsentCount = absentCount,
                    LateCount = lateCount,
                    AttendancePercentage = percentage
                });
            }

            return Result<List<AttendanceReportDto>>.SuccessResult(
                reports.OrderBy(r => r.StudentName).ToList());
        }
        catch (Exception ex)
        {
            return Result<List<AttendanceReportDto>>.FailureResult(
                $"An error occurred while generating course report: {ex.Message}");
        }
    }

    public async Task<Result<AttendanceReportDto>> GetStudentCourseAttendanceReportAsync(
        int studentId, int courseId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var attendanceRepo = _unitOfWork.Repository<Attendance>();
            var courseRepo = _unitOfWork.Repository<Course>();
            var userRepo = _unitOfWork.Repository<User>();

            var student = await userRepo.GetByIdAsync(studentId);
            if (student == null)
            {
                return Result<AttendanceReportDto>.FailureResult("Student not found");
            }

            var course = await courseRepo.GetByIdAsync(courseId);
            if (course == null)
            {
                return Result<AttendanceReportDto>.FailureResult("Course not found");
            }

            var attendances = await attendanceRepo.FindAsync(a =>
                a.StudentId == studentId && a.CourseId == courseId);

            // Apply date filters
            if (startDate.HasValue)
            {
                attendances = attendances.Where(a => a.Date >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                attendances = attendances.Where(a => a.Date <= endDate.Value);
            }

            var attendanceList = attendances.ToList();
            var totalClasses = attendanceList.Count;
            var presentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Present);
            var absentCount = attendanceList.Count(a => a.Status == AttendanceStatus.Absent);
            var lateCount = attendanceList.Count(a => a.Status == AttendanceStatus.Late);

            var percentage = totalClasses > 0
                ? Math.Round((decimal)(presentCount + lateCount) / totalClasses * 100, 2)
                : 0;

            var report = new AttendanceReportDto
            {
                StudentName = $"{student.FirstName} {student.LastName}",
                RegistrationNumber = student.RegistrationNumber ?? "N/A",
                CourseName = course.CourseName,
                TotalClasses = totalClasses,
                PresentCount = presentCount,
                AbsentCount = absentCount,
                LateCount = lateCount,
                AttendancePercentage = percentage
            };

            return Result<AttendanceReportDto>.SuccessResult(report);
        }
        catch (Exception ex)
        {
            return Result<AttendanceReportDto>.FailureResult(
                $"An error occurred while generating student course report: {ex.Message}");
        }
    }
}