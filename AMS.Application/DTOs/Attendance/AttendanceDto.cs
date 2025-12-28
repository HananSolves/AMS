using AMS.Core.Enums;

namespace AMS.Application.DTOs.Attendance;

public class AttendanceDto
{
    public int Id { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}