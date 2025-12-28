using System.Security.Claims;
using AMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IAttendanceService _attendanceService;
    private readonly IEnrollmentService _enrollmentService;

    public DashboardController(
        ICourseService courseService,
        IAttendanceService attendanceService,
        IEnrollmentService enrollmentService)
    {
        _courseService = courseService;
        _attendanceService = attendanceService;
        _enrollmentService = enrollmentService;
    }

    public async Task<IActionResult> Index()
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userId = GetUserId();

        if (userRole == "Student")
        {
            return await StudentDashboard(userId);
        }
        else if (userRole == "Teacher")
        {
            return await TeacherDashboard(userId);
        }
        else if (userRole == "Admin")
        {
            return await AdminDashboard();
        }

        return View("Error");
    }
    private async Task<IActionResult> StudentDashboard(int userId)
    {
        var enrollmentsResult = await _enrollmentService.GetStudentEnrollmentsAsync(userId);
        var attendanceResult = await _attendanceService.GetStudentAttendanceAsync(userId);

        ViewBag.Enrollments = enrollmentsResult.Data ?? new List<AMS.Application.DTOs.Enrollment.EnrollmentDto>();
        ViewBag.RecentAttendance = attendanceResult.Data?.Take(5).ToList() ?? new List<AMS.Application.DTOs.Attendance.AttendanceDto>();

        return View("Student");
    }

    private async Task<IActionResult> TeacherDashboard(int userId)
    {
        var coursesResult = await _courseService.GetTeacherCoursesAsync(userId);

        var courses = coursesResult.Data ?? new List<AMS.Application.DTOs.Course.CourseDto>();

        // calculate small summary stats for the teacher dashboard
        ViewBag.Courses = courses;
        ViewBag.TotalCourses = courses.Count;
        try
        {
            ViewBag.TotalStudents = courses.Sum(c => c.EnrolledStudents);
        }
        catch
        {
            ViewBag.TotalStudents = 0;
        }

        return View("Teacher");
    }

    
    private async Task<IActionResult> AdminDashboard()
    {
        // Fetch all courses so admin can view/edit/delete them from the dashboard
        var coursesResult = await _courseService.GetAllCoursesAsync();
        var courses = coursesResult.Data ?? new List<AMS.Application.DTOs.Course.CourseDto>();

        ViewBag.Courses = courses;
        ViewBag.TotalCourses = courses.Count;
        try
        {
            ViewBag.TotalStudents = courses.Sum(c => c.EnrolledStudents);
        }
        catch
        {
            ViewBag.TotalStudents = 0;
        }

        return View("Admin");
    }
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 0;
    }
}