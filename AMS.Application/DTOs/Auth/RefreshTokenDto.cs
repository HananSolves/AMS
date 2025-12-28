using System.ComponentModel.DataAnnotations;

namespace AMS.Application.DTOs.Auth;

public class RefreshTokenDto
{
    [Required(ErrorMessage = "Refresh token is required")]
    public string RefreshToken { get; set; } = string.Empty;
}