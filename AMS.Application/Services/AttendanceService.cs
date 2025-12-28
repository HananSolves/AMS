using AMS.Application.Common;
using AMS.Application.DTOs.Attendance;
using AMS.Core.Entities;
using AMS.Core.Enums;
using AMS.Core.Interfaces;

namespace AMS.Application.Services;

public class AttendanceService : IAttendanceService
{
    private readonly IUnitOfWork _unitOfWork;

    public AttendanceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    public async Task<Result<bool>> MarkAttendanceAsync(MarkAttendanceDto markAttendanceDto, int teacherId)
{
    try
    {
        // Remove manual transaction - let EF Core handle it
        // await _unitOfWork.BeginTransactionAsync();

        // Verify course exists and teacher is assigned
        var courseRepo = _unitOfWork.Repository<Course>();
        var course = await courseRepo.GetByIdAsync(markAttendanceDto.CourseId);

        if (course == null)
        {
            return Result<bool>.FailureResult("Course not found");
        }

        if (course.TeacherId != teacherId)
        {
            return Result<bool>.FailureResult("You are not authorized to mark attendance for this course");
        }

        var attendanceRepo = _unitOfWork.Repository<Attendance>();
        var enrollmentRepo = _unitOfWork.Repository<Enrollment>();
        var userRepo = _unitOfWork.Repository<User>();

        // Check for duplicate attendance entries for the same date
        var existingAttendance = await attendanceRepo.FindAsync(a =>
            a.CourseId == markAttendanceDto.CourseId &&
            a.Date.Date == markAttendanceDto.Date.Date);

        if (existingAttendance.Any())
        {
            return Result<bool>.FailureResult(
                $"Attendance has already been marked for {markAttendanceDto.Date:yyyy-MM-dd}");
        }

        var attendanceRecords = new List<Attendance>();

        foreach (var studentAttendance in markAttendanceDto.Students)
        {
            // Verify student is enrolled in the course
            var enrollment = await enrollmentRepo.FirstOrDefaultAsync(e =>
                e.StudentId == studentAttendance.StudentId &&
                e.CourseId == markAttendanceDto.CourseId &&
                e.IsActive);

            if (enrollment == null)
            {
                return Result<bool>.FailureResult(
                    $"Student ID {studentAttendance.StudentId} is not enrolled in this course");
            }

            var attendance = new Attendance
            {
                StudentId = studentAttendance.StudentId,
                CourseId = markAttendanceDto.CourseId,
                Date = markAttendanceDto.Date.Date,
                Status = studentAttendance.Status,
                Remarks = studentAttendance.Remarks?.Trim(),
                MarkedAt = DateTime.UtcNow,
                MarkedBy = teacherId
            };

            attendanceRecords.Add(attendance);
        }

        await attendanceRepo.AddRangeAsync(attendanceRecords);
        await _unitOfWork.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Attendance marked successfully");
    }
    catch (Exception ex)
    {
        return Result<bool>.FailureResult(
            $"An error occurred while marking attendance: {ex.Message}");
    }
}
    public async Task<Result<List<AttendanceDto>>> GetStudentAttendanceAsync(int studentId, int? courseId = null)
    {
        try
        {
            var attendanceRepo = _unitOfWork.Repository<Attendance>();
            var query = attendanceRepo.FindAsync(a => a.StudentId == studentId);

            var attendances = await query;

            if (courseId.HasValue)
            {
                attendances = attendances.Where(a => a.CourseId == courseId.Value);
            }

            var courseRepo = _unitOfWork.Repository<Course>();
            var userRepo = _unitOfWork.Repository<User>();
            var student = await userRepo.GetByIdAsync(studentId);

            var attendanceDtos = new List<AttendanceDto>();

            foreach (var attendance in attendances.OrderByDescending(a => a.Date))
            {
                var course = await courseRepo.GetByIdAsync(attendance.CourseId);

                attendanceDtos.Add(new AttendanceDto
                {
                    Id = attendance.Id,
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
                    RegistrationNumber = student?.RegistrationNumber ?? "N/A",
                    CourseName = course?.CourseName ?? "Unknown",
                    Date = attendance.Date,
                    Status = attendance.Status,
                    StatusText = attendance.Status.ToString(),
                    Remarks = attendance.Remarks
                });
            }

            return Result<List<AttendanceDto>>.SuccessResult(attendanceDtos);
        }
        catch (Exception ex)
        {
            return Result<List<AttendanceDto>>.FailureResult(
                $"An error occurred while retrieving attendance: {ex.Message}");
        }
    }

    public async Task<Result<List<AttendanceDto>>> GetCourseAttendanceAsync(int courseId, DateTime? date = null)
    {
        try
        {
            var attendanceRepo = _unitOfWork.Repository<Attendance>();
            var attendances = await attendanceRepo.FindAsync(a => a.CourseId == courseId);

            if (date.HasValue)
            {
                attendances = attendances.Where(a => a.Date.Date == date.Value.Date);
            }

            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(courseId);

            var userRepo = _unitOfWork.Repository<User>();
            var attendanceDtos = new List<AttendanceDto>();

            foreach (var attendance in attendances.OrderByDescending(a => a.Date))
            {
                var student = await userRepo.GetByIdAsync(attendance.StudentId);

                attendanceDtos.Add(new AttendanceDto
                {
                    Id = attendance.Id,
                    StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
                    RegistrationNumber = student?.RegistrationNumber ?? "N/A",
                    CourseName = course?.CourseName ?? "Unknown",
                    Date = attendance.Date,
                    Status = attendance.Status,
                    StatusText = attendance.Status.ToString(),
                    Remarks = attendance.Remarks
                });
            }

            return Result<List<AttendanceDto>>.SuccessResult(attendanceDtos);
        }
        catch (Exception ex)
        {
            return Result<List<AttendanceDto>>.FailureResult(
                $"An error occurred while retrieving course attendance: {ex.Message}");
        }
    }

    public async Task<Result<AttendanceDto>> UpdateAttendanceAsync(int attendanceId, AttendanceDto attendanceDto, int teacherId)
    {
        try
        {
            var attendanceRepo = _unitOfWork.Repository<Attendance>();
            var attendance = await attendanceRepo.GetByIdAsync(attendanceId);

            if (attendance == null)
            {
                return Result<AttendanceDto>.FailureResult("Attendance record not found");
            }

            // Verify teacher is assigned to the course
            var courseRepo = _unitOfWork.Repository<Course>();
            var course = await courseRepo.GetByIdAsync(attendance.CourseId);

            if (course == null || course.TeacherId != teacherId)
            {
                return Result<AttendanceDto>.FailureResult(
                    "You are not authorized to update this attendance record");
            }

            attendance.Status = attendanceDto.Status;
            attendance.Remarks = attendanceDto.Remarks?.Trim();
            attendanceRepo.Update(attendance);
            await _unitOfWork.SaveChangesAsync();

            var userRepo = _unitOfWork.Repository<User>();
            var student = await userRepo.GetByIdAsync(attendance.StudentId);

            var updatedDto = new AttendanceDto
            {
                Id = attendance.Id,
                StudentName = student != null ? $"{student.FirstName} {student.LastName}" : "Unknown",
                RegistrationNumber = student?.RegistrationNumber ?? "N/A",
                CourseName = course.CourseName,
                Date = attendance.Date,
                Status = attendance.Status,
                StatusText = attendance.Status.ToString(),
                Remarks = attendance.Remarks
            };

            return Result<AttendanceDto>.SuccessResult(updatedDto, "Attendance updated successfully");
        }
        catch (Exception ex)
        {
            return Result<AttendanceDto>.FailureResult(
                $"An error occurred while updating attendance: {ex.Message}");
        }
    }
}