// AMS.Web/Controllers/CourseController.cs
using System.Security.Claims;
using AMS.Application.DTOs.Course;
using AMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize]
public class CourseController : Controller
{
    private readonly ICourseService _courseService;
    private readonly IAuthService _authService;

    public CourseController(ICourseService courseService, IAuthService authService)
    {
        _courseService = courseService;
        _authService = authService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        var result = await _courseService.GetAllCoursesAsync(userId, userRole);
        
        return View(result.Data ?? new List<CourseDto>());
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        if (User.IsInRole("Teacher"))
        {
            ViewBag.IsTeacher = true;
            ViewBag.TeacherId = GetUserId();
        }
        else if (User.IsInRole("Admin"))
        {
            ViewBag.IsTeacher = false;
            var teachersResult = await _authService.GetAllTeachersAsync();
            ViewBag.Teachers = teachersResult.Data ?? new List<AMS.Application.DTOs.User.UserDto>();
        }
        
        return View();
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateCourseDto model)
    {
        // If teacher is creating, override the TeacherId with their own ID
        if (User.IsInRole("Teacher"))
        {
            model.TeacherId = GetUserId();
        }
        
        if (!ModelState.IsValid)
        {
            // Re-populate ViewBag for dropdown
            if (User.IsInRole("Admin"))
            {
                ViewBag.IsTeacher = false;
                var teachersResult = await _authService.GetAllTeachersAsync();
                ViewBag.Teachers = teachersResult.Data ?? new List<AMS.Application.DTOs.User.UserDto>();
            }
            return View(model);
        }

        var result = await _courseService.CreateCourseAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            // Re-populate ViewBag for dropdown
            if (User.IsInRole("Admin"))
            {
                ViewBag.IsTeacher = false;
                var teachersResult = await _authService.GetAllTeachersAsync();
                ViewBag.Teachers = teachersResult.Data ?? new List<AMS.Application.DTOs.User.UserDto>();
            }
            return View(model);
        }

        TempData["SuccessMessage"] = "Course created successfully!";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var userId = GetUserId();
        var result = await _courseService.GetCourseByIdAsync(id, userId);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
            return RedirectToAction(nameof(Index));
        }

        return View(result.Data);
    }

    [Authorize(Roles = "Teacher,Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        
        var result = await _courseService.DeleteCourseAsync(id, userId, userRole);

        if (!result.Success)
        {
            TempData["ErrorMessage"] = result.Message;
        }
        else
        {
            TempData["SuccessMessage"] = "Course deleted successfully!";
        }

        return RedirectToAction(nameof(Index));
    }

    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId) ? userId : 0;
    }
}