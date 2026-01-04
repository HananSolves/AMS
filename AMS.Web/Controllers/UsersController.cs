using AMS.Application.DTOs.Auth;
using AMS.Application.DTOs.User;
using AMS.Core.Entities;
using AMS.Core.Enums;
using AMS.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly AMS.Application.Services.IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUnitOfWork unitOfWork, AMS.Application.Services.IAuthService authService, ILogger<UsersController> logger)
    {
        _unitOfWork = unitOfWork;
        _authService = authService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var userRepo = _unitOfWork.Repository<User>();
        // Only show active users in the admin list. Deleted users are soft-deactivated (IsActive = false).
        var users = (await userRepo.FindAsync(u => u.IsActive)).ToList();

        var dtos = users.Select(u => new UserDto
        {
            Id = u.Id,
            FirstName = u.FirstName,
            LastName = u.LastName,
            Email = u.Email,
            Role = u.Role.ToString()
        }).ToList();

        return View(dtos);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new RegisterDto { Role = UserRole.Student };
        return View(model);
    }

    [HttpPost]
    // [ValidateAntiForgeryToken] - REMOVED: Antiforgery validation disabled globally
    public async Task<IActionResult> Create(RegisterDto model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var result = await _authService.RegisterAsync(model);
        if (result.Success)
        {
            return RedirectToAction(nameof(Index));
        }

        ModelState.AddModelError(string.Empty, result.Message ?? "Unable to create user");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(id);
        if (user == null)
            return NotFound();

        var vm = new AMS.Web.Models.UserEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive
        };

        return View(vm);
    }

    [HttpPost]
    // [ValidateAntiForgeryToken] - REMOVED: Antiforgery validation disabled globally
    public async Task<IActionResult> Edit(AMS.Web.Models.UserEditViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Email = model.Email.Trim().ToLower();
        user.Role = model.Role;
        user.IsActive = model.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    // [ValidateAntiForgeryToken] - REMOVED: Antiforgery validation disabled globally
    public async Task<IActionResult> Delete(int id)
    {
        var userRepo = _unitOfWork.Repository<User>();
        var user = await userRepo.GetByIdAsync(id);
        if (user == null) return NotFound();

        // Soft-delete: deactivate account
        user.IsActive = false;
        user.UpdatedAt = DateTime.UtcNow;

        userRepo.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}