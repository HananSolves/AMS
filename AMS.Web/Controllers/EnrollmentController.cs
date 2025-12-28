using System.Security.Claims;
using AMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize(Roles = "Student")]
public class EnrollmentController : Controller
{
    private readonly IEnrollmentService _enrollmentService;
    private readonly ICourseService _courseService;

    public EnrollmentController(
        IEnrollmentService enrollmentService,
        ICourseService courseService)
    {
        _enrollmentService = enrollmentService;
        _courseService = courseService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Enroll(int courseId)
    {
        var studentId = GetUserId();
        var result = await _enrollmentService.EnrollStudentAsync(studentId, courseId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Successfully enrolled in course!";
        }

        return RedirectToAction("Index", "Course");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unenroll(int courseId)
    {
        var studentId = GetUserId();
        var result = await _enrollmentService.UnenrollStudentAsync(studentId, courseId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Successfully unenrolled from course!";
        }

        return RedirectToAction("Index", "Course");
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 0;
    }
}