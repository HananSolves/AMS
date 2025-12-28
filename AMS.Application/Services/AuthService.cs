using AMS.Application.Common;
using AMS.Application.DTOs.Auth;
using AMS.Application.DTOs.User;
using AMS.Application.Helpers;
using AMS.Core.Entities;
using AMS.Core.Enums;
using AMS.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace AMS.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly JwtHelper _jwtHelper;
    private readonly JwtSettings _jwtSettings;

    public AuthService(
        IUnitOfWork unitOfWork,
        JwtHelper jwtHelper,
        IOptions<JwtSettings> jwtSettings)
    {
        _unitOfWork = unitOfWork;
        _jwtHelper = jwtHelper;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<TokenDto>> LoginAsync(LoginDto loginDto)
    {
        try
        {
            // Find user by email
            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == loginDto.Email.ToLower());

            if (user == null)
            {
                return Result<TokenDto>.FailureResult("Invalid email or password");
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Result<TokenDto>.FailureResult("Account is deactivated. Please contact administrator.");
            }

            // Verify password
            if (!PasswordHelper.VerifyPassword(loginDto.Password, user.PasswordHash))
            {
                return Result<TokenDto>.FailureResult("Invalid email or password");
            }

            // Generate tokens
            var accessToken = _jwtHelper.GenerateAccessToken(user);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            await tokenRepo.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var tokenDto = new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    RegistrationNumber = user.RegistrationNumber
                }
            };

            return Result<TokenDto>.SuccessResult(tokenDto, "Login successful");
        }
        catch (Exception ex)
        {
            return Result<TokenDto>.FailureResult($"An error occurred during login: {ex.Message}");
        }
    }

    public async Task<Result<TokenDto>> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();

            // Check if email already exists
            var existingUser = await userRepo.FirstOrDefaultAsync(u => 
                u.Email.ToLower() == registerDto.Email.ToLower());

            if (existingUser != null)
            {
                return Result<TokenDto>.FailureResult("Email already registered");
            }

            // Validate registration number for students
            if (registerDto.Role == UserRole.Student)
            {
                if (string.IsNullOrWhiteSpace(registerDto.RegistrationNumber))
                {
                    return Result<TokenDto>.FailureResult("Registration number is required for students");
                }

                // Check if registration number already exists
                var existingRegNo = await userRepo.FirstOrDefaultAsync(u => 
                    u.RegistrationNumber == registerDto.RegistrationNumber);

                if (existingRegNo != null)
                {
                    return Result<TokenDto>.FailureResult("Registration number already exists");
                }
            }

            // Validate password strength
            if (!PasswordHelper.ValidatePasswordStrength(registerDto.Password))
            {
                return Result<TokenDto>.FailureResult(
                    "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character");
            }

            // Create new user
            var user = new User
            {
                FirstName = registerDto.FirstName.Trim(),
                LastName = registerDto.LastName.Trim(),
                Email = registerDto.Email.ToLower().Trim(),
                PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
                Role = registerDto.Role,
                RegistrationNumber = registerDto.Role == UserRole.Student 
                    ? registerDto.RegistrationNumber?.Trim().ToUpper() 
                    : null,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await userRepo.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();

            // Generate tokens
            var accessToken = _jwtHelper.GenerateAccessToken(user);
            var refreshToken = _jwtHelper.GenerateRefreshToken();

            // Save refresh token
            var refreshTokenEntity = new RefreshToken
            {
                Token = refreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            await tokenRepo.AddAsync(refreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var tokenDto = new TokenDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    RegistrationNumber = user.RegistrationNumber
                }
            };

            return Result<TokenDto>.SuccessResult(tokenDto, "Registration successful");
        }
        catch (Exception ex)
        {
            return Result<TokenDto>.FailureResult($"An error occurred during registration: {ex.Message}");
        }
    }

    public async Task<Result<TokenDto>> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            var token = await tokenRepo.FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (token == null || !token.IsActive)
            {
                return Result<TokenDto>.FailureResult("Invalid or expired refresh token");
            }

            var userRepo = _unitOfWork.Repository<User>();
            var user = await userRepo.GetByIdAsync(token.UserId);

            if (user == null || !user.IsActive)
            {
                return Result<TokenDto>.FailureResult("User not found or inactive");
            }

            // Revoke old refresh token
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;

            // Generate new tokens
            var newAccessToken = _jwtHelper.GenerateAccessToken(user);
            var newRefreshToken = _jwtHelper.GenerateRefreshToken();

            // Save new refresh token
            var newRefreshTokenEntity = new RefreshToken
            {
                Token = newRefreshToken,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
                CreatedAt = DateTime.UtcNow
            };

            token.ReplacedByToken = newRefreshToken;
            await tokenRepo.AddAsync(newRefreshTokenEntity);
            await _unitOfWork.SaveChangesAsync();

            var tokenDto = new TokenDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.AccessTokenExpirationMinutes),
                User = new UserInfoDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    RegistrationNumber = user.RegistrationNumber
                }
            };

            return Result<TokenDto>.SuccessResult(tokenDto, "Token refreshed successfully");
        }
        catch (Exception ex)
        {
            return Result<TokenDto>.FailureResult($"An error occurred while refreshing token: {ex.Message}");
        }
    }

    public async Task<Result<bool>> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            var token = await tokenRepo.FirstOrDefaultAsync(t => t.Token == refreshToken);

            if (token == null)
            {
                return Result<bool>.FailureResult("Token not found");
            }

            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync();

            return Result<bool>.SuccessResult(true, "Token revoked successfully");
        }
        catch (Exception ex)
        {
            return Result<bool>.FailureResult($"An error occurred while revoking token: {ex.Message}");
        }
    }

    public async Task<Result<bool>> LogoutAsync(int userId)
    {
        try
        {
            var tokenRepo = _unitOfWork.Repository<RefreshToken>();
            var userTokens = await tokenRepo.FindAsync(t => 
                t.UserId == userId && t.IsActive);

            foreach (var token in userTokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync();
            return Result<bool>.SuccessResult(true, "Logout successful");
        }
        catch (Exception ex)
        {
            return Result<bool>.FailureResult($"An error occurred during logout: {ex.Message}");
        }
    }
    
    // Add to AuthService.cs
    public async Task<Result<List<UserDto>>> GetAllTeachersAsync()
    {
        try
        {
            var userRepo = _unitOfWork.Repository<User>();
            var teachers = await userRepo.FindAsync(u => u.Role == UserRole.Teacher && u.IsActive);

            var teacherDtos = teachers.Select(t => new UserDto
            {
                Id = t.Id,
                FirstName = t.FirstName,
                LastName = t.LastName,
                Email = t.Email,
                Role = t.Role.ToString()
            }).ToList();

            return Result<List<UserDto>>.SuccessResult(teacherDtos);
        }
        catch (Exception ex)
        {
            return Result<List<UserDto>>.FailureResult($"Error retrieving teachers: {ex.Message}");
        }
    }
}