// AMS.Web/Controllers/ReportController.cs
using System.Security.Claims;
using AMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize]
public class ReportController : Controller
{
    private readonly IReportService _reportService;
    private readonly ICourseService _courseService;
    private readonly IPdfService _pdfService;

    public ReportController(
        IReportService reportService,
        ICourseService courseService,
        IPdfService pdfService)
    {
        _reportService = reportService;
        _courseService = courseService;
        _pdfService = pdfService;
    }

    public async Task<IActionResult> Index(int? courseId, DateTime? startDate, DateTime? endDate)
    {
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Student")
        {
            var result = await _reportService.GetStudentAttendanceReportAsync(
                userId, courseId, startDate, endDate);
            
            var enrollments = await _courseService.GetAllCoursesAsync(userId, userRole);
            ViewBag.Courses = enrollments.Data?.Where(c => c.IsEnrolled).ToList();
            ViewBag.UserRole = "Student";
            ViewBag.SelectedCourseId = courseId;
            return View(result.Data ?? new List<AMS.Application.DTOs.Attendance.AttendanceReportDto>());
        }
        else if (userRole == "Teacher")
        {
            if (!courseId.HasValue)
            {
                var coursesResult = await _courseService.GetTeacherCoursesAsync(userId);
                ViewBag.Courses = coursesResult.Data;
                ViewBag.UserRole = "Teacher";
                return View(new List<AMS.Application.DTOs.Attendance.AttendanceReportDto>());
            }

            var result = await _reportService.GetCourseAttendanceReportAsync(
                courseId.Value, startDate, endDate);
            
            var coursesResult2 = await _courseService.GetTeacherCoursesAsync(userId);
            ViewBag.Courses = coursesResult2.Data;
            ViewBag.UserRole = "Teacher";
            ViewBag.SelectedCourseId = courseId;
            return View(result.Data ?? new List<AMS.Application.DTOs.Attendance.AttendanceReportDto>());
        }
        else if (userRole == "Admin")
        {
            if (!courseId.HasValue)
            {
                var coursesResult = await _courseService.GetAllCoursesAsync();
                ViewBag.Courses = coursesResult.Data;
                ViewBag.UserRole = "Admin";
                return View(new List<AMS.Application.DTOs.Attendance.AttendanceReportDto>());
            }

            var result = await _reportService.GetCourseAttendanceReportAsync(
                courseId.Value, startDate, endDate);
            
            var coursesResult2 = await _courseService.GetAllCoursesAsync();
            ViewBag.Courses = coursesResult2.Data;
            ViewBag.UserRole = "Admin";
            ViewBag.SelectedCourseId = courseId;
            return View(result.Data ?? new List<AMS.Application.DTOs.Attendance.AttendanceReportDto>());
        }

        return View("Error");
    }

    [HttpPost]
    public async Task<IActionResult> ExportPdf(int? courseId, DateTime? startDate, DateTime? endDate)
    {
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        List<AMS.Application.DTOs.Attendance.AttendanceReportDto>? reports = null;
        string title = "Attendance Report";

        if (userRole == "Student")
        {
            var result = await _reportService.GetStudentAttendanceReportAsync(
                userId, courseId, startDate, endDate);
            reports = result.Data;
            title = "Student Attendance Report";
        }
        else if (userRole == "Teacher" && courseId.HasValue)
        {
            var result = await _reportService.GetCourseAttendanceReportAsync(
                courseId.Value, startDate, endDate);
            reports = result.Data;
            title = "Course Attendance Report";
        }
        else if (userRole == "Admin" && courseId.HasValue)
        {
            var result = await _reportService.GetCourseAttendanceReportAsync(
                courseId.Value, startDate, endDate);
            reports = result.Data;
            title = "Course Attendance Report (Admin)";
        }

        if (reports == null || !reports.Any())
        {
            TempData["ErrorMessage"] = "No data available to export";
            return RedirectToAction(nameof(Index), new { courseId, startDate, endDate });
        }

        var pdfBytes = _pdfService.GenerateAttendanceReportPdf(reports, title);
        
        return File(pdfBytes, "application/pdf", $"AttendanceReport_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 0;
    }
}