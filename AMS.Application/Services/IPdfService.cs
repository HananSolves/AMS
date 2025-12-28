namespace AMS.Application.Services;

public interface IPdfService
{
    byte[] GenerateAttendanceReportPdf(List<AMS.Application.DTOs.Attendance.AttendanceReportDto> reports, string title);
}