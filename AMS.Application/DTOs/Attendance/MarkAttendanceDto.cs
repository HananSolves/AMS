using System.ComponentModel.DataAnnotations;
using AMS.Core.Enums;

namespace AMS.Application.DTOs.Attendance;

public class MarkAttendanceDto
{
    [Required]
    public int CourseId { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Required]
    public List<StudentAttendanceDto> Students { get; set; } = new();
}

public class StudentAttendanceDto
{
    [Required]
    public int StudentId { get; set; }

    [Required]
    public AttendanceStatus Status { get; set; }

    [StringLength(200)]
    public string? Remarks { get; set; }
}