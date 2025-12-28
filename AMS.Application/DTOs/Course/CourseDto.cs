namespace AMS.Application.DTOs.Course;

public class CourseDto
{
    public int Id { get; set; }
    public string CourseCode { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CreditHours { get; set; }
    public string TeacherName { get; set; } = string.Empty;
    public int TeacherId { get; set; }
    public int EnrolledStudents { get; set; }
    public bool IsEnrolled { get; set; }
    public DateTime CreatedAt { get; set; }
}