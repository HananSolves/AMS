namespace AMS.Application.DTOs.Attendance;

public class AttendanceReportDto
{
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public int TotalClasses { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public decimal AttendancePercentage { get; set; }
}