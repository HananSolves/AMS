using AMS.Core.Enums;
using System.ComponentModel.DataAnnotations;

namespace AMS.Web.Models;

public class UserEditViewModel
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; }

    public bool IsActive { get; set; } = true;
}
