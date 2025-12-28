using System.ComponentModel.DataAnnotations;

namespace AMS.Application.DTOs.Enrollment;

public class EnrollmentDto
{
    [Required(ErrorMessage = "Course selection is required")]
    public int CourseId { get; set; }
    
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string RegistrationNumber { get; set; } = string.Empty;
    public string CourseName { get; set; } = string.Empty;
    public string CourseCode { get; set; } = string.Empty;
    public DateTime EnrolledAt { get; set; }
}