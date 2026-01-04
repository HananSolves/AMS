using System.Security.Claims;
using AMS.Application.DTOs.Attendance;
using AMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize]
public class AttendanceController : Controller
{
    private readonly IAttendanceService _attendanceService;
    private readonly ICourseService _courseService;
    private readonly IEnrollmentService _enrollmentService;

    public AttendanceController(
        IAttendanceService attendanceService,
        ICourseService courseService,
        IEnrollmentService enrollmentService)
    {
        _attendanceService = attendanceService;
        _courseService = courseService;
        _enrollmentService = enrollmentService;
    }

    [Authorize(Roles = "Teacher")]
    [HttpGet]
    public async Task<IActionResult> Mark(int courseId)
    {
        var teacherId = GetUserId();
        var courseResult = await _courseService.GetCourseByIdAsync(courseId);

        if (!courseResult.Success)
        {
            TempData["ErrorMessage"] = "Course not found";
            return RedirectToAction("Index", "Dashboard");
        }

        var course = courseResult.Data!;
    
        if (course.TeacherId != teacherId)
        {
            TempData["ErrorMessage"] = "You are not authorized to mark attendance for this course";
            return RedirectToAction("Index", "Dashboard");
        }

        var enrollmentsResult = await _enrollmentService.GetCourseEnrollmentsAsync(courseId);
    
        ViewBag.Course = course;
        ViewBag.Enrollments = enrollmentsResult.Data ?? new List<AMS.Application.DTOs.Enrollment.EnrollmentDto>();

        return View();
    }

    [Authorize(Roles = "Teacher")]
    [HttpPost]
    // [ValidateAntiForgeryToken] - REMOVED: Antiforgery validation disabled globally
    public async Task<IActionResult> Mark(MarkAttendanceDto model)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var teacherId = GetUserId();
        var result = await _attendanceService.MarkAttendanceAsync(model, teacherId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Attendance marked successfully!";
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize(Roles = "Student")]
    public async Task<IActionResult> ViewAttendance()
    {
        var studentId = GetUserId();
        var result = await _attendanceService.GetStudentAttendanceAsync(studentId);

        return View(result.Data ?? new List<AttendanceDto>());
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 0;
    }
}