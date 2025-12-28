using AMS.Core.Enums;

namespace AMS.Core.Entities;

public class Attendance
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public int CourseId { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string? Remarks { get; set; }
    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
    public int MarkedBy { get; set; } // Teacher ID

    // Navigation properties
    public User Student { get; set; } = null!;
    public Course Course { get; set; } = null!;
}