using AMS.Application.Common;
using AMS.Application.DTOs.Auth;
using AMS.Application.DTOs.User;

namespace AMS.Application.Services;

public interface IAuthService
{
    Task<Result<TokenDto>> LoginAsync(LoginDto loginDto);
    Task<Result<TokenDto>> RegisterAsync(RegisterDto registerDto);
    Task<Result<TokenDto>> RefreshTokenAsync(string refreshToken);
    Task<Result<bool>> RevokeTokenAsync(string refreshToken);
    Task<Result<bool>> LogoutAsync(int userId);
    
    Task<Result<List<UserDto>>> GetAllTeachersAsync();
}