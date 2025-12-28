using System.ComponentModel.DataAnnotations;

namespace AMS.Application.DTOs.Course;

public class CreateCourseDto
{
    [Required(ErrorMessage = "Course code is required")]
    [RegularExpression(@"^[A-Z]{3}-\d{3}$", 
        ErrorMessage = "Course code must be in format: ABC-123")]
    public string CourseCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course name is required")]
    [StringLength(100, ErrorMessage = "Course name cannot exceed 100 characters")]
    public string CourseName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required")]
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Credit hours are required")]
    [Range(1, 6, ErrorMessage = "Credit hours must be between 1 and 6")]
    public int CreditHours { get; set; }

    [Required(ErrorMessage = "Teacher is required")]
    public int TeacherId { get; set; }
}