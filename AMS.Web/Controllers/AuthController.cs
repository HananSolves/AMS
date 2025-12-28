using System.Security.Claims;
using AMS.Application.DTOs.Auth;
using AMS.Application.Services;
using AMS.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

public class AuthController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        // If already authenticated, redirect to appropriate dashboard
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginDto model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _authService.LoginAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        // Store tokens in cookies
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Use true in production with HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = result.Data!.ExpiresAt
        };

        Response.Cookies.Append("AccessToken", result.Data.AccessToken, cookieOptions);
        Response.Cookies.Append("RefreshToken", result.Data.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        // Store user info in session
        HttpContext.Session.SetString("UserId", result.Data.User.Id.ToString());
        HttpContext.Session.SetString("UserRole", result.Data.User.Role);
        HttpContext.Session.SetString("UserName", $"{result.Data.User.FirstName} {result.Data.User.LastName}");

        TempData["SuccessMessage"] = "Login successful!";

        // Redirect to return URL or dashboard
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Dashboard");
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Validate registration number for students
        if (model.Role == UserRole.Student && string.IsNullOrWhiteSpace(model.RegistrationNumber))
        {
            ModelState.AddModelError("RegistrationNumber", "Registration number is required for students");
            return View(model);
        }

        var result = await _authService.RegisterAsync(model);

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            if (result.Errors.Any())
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error);
                }
            }
            return View(model);
        }

        // Store tokens in cookies
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = result.Data!.ExpiresAt
        };

        Response.Cookies.Append("AccessToken", result.Data.AccessToken, cookieOptions);
        Response.Cookies.Append("RefreshToken", result.Data.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        // Store user info in session
        HttpContext.Session.SetString("UserId", result.Data.User.Id.ToString());
        HttpContext.Session.SetString("UserRole", result.Data.User.Role);
        HttpContext.Session.SetString("UserName", $"{result.Data.User.FirstName} {result.Data.User.LastName}");

        TempData["SuccessMessage"] = "Registration successful! Welcome to AMS.";

        return RedirectToAction("Index", "Dashboard");
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
        {
            await _authService.LogoutAsync(userId);
        }

        // Clear cookies
        Response.Cookies.Delete("AccessToken");
        Response.Cookies.Delete("RefreshToken");

        // Clear session
        HttpContext.Session.Clear();

        TempData["SuccessMessage"] = "You have been logged out successfully.";

        return RedirectToAction("Login");
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> RefreshToken()
    {
        var refreshToken = Request.Cookies["RefreshToken"];

        if (string.IsNullOrEmpty(refreshToken))
        {
            return Json(new { success = false, message = "Refresh token not found" });
        }

        var result = await _authService.RefreshTokenAsync(refreshToken);

        if (!result.Success)
        {
            return Json(new { success = false, message = result.Message });
        }

        // Update cookies with new tokens
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = result.Data!.ExpiresAt
        };

        Response.Cookies.Append("AccessToken", result.Data.AccessToken, cookieOptions);
        Response.Cookies.Append("RefreshToken", result.Data.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTimeOffset.UtcNow.AddDays(7)
        });

        return Json(new { success = true, message = "Token refreshed successfully" });
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }
}