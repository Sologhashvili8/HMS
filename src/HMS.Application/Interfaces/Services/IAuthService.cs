using HMS.Application.DTOs.Auth;

namespace HMS.Application.Interfaces.Services;

public interface IAuthService
{
    Task RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto);
    Task ResendVerificationAsync(ResendVerificationDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task ForgotPasswordAsync(ForgotPasswordDto dto);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task SendChangePasswordCodeAsync();
    Task ChangePasswordAsync(ChangePasswordDto dto);
}
