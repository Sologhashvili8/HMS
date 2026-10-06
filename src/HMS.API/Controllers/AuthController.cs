using HMS.Application.Common;
using HMS.Application.DTOs.Auth;
using HMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HMS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<object>>> Register(RegisterDto dto)
    {
        await _authService.RegisterAsync(dto);
        return Ok(ApiResponse<object>.SuccessResponse(new { }));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> VerifyEmail(VerifyEmailDto dto)
    {
        var result = await _authService.VerifyEmailAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result));
    }

    [HttpPost("resend-verification")]
    public async Task<ActionResult<ApiResponse<object>>> ResendVerification(ResendVerificationDto dto)
    {
        await _authService.ResendVerificationAsync(dto);
        return Ok(ApiResponse<object>.SuccessResponse(new { }));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(ApiResponse<AuthResponseDto>.SuccessResponse(result));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword(ForgotPasswordDto dto)
    {
        await _authService.ForgotPasswordAsync(dto);
        return Ok(ApiResponse<object>.SuccessResponse(new { }));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword(ResetPasswordDto dto)
    {
        await _authService.ResetPasswordAsync(dto);
        return Ok(ApiResponse<object>.SuccessResponse(new { }));
    }

    [HttpPost("change-password/send-code")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> SendChangePasswordCode()
    {
        await _authService.SendChangePasswordCodeAsync();
        return Ok(ApiResponse<object>.SuccessResponse(new { }));
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword(ChangePasswordDto dto)
    {
        await _authService.ChangePasswordAsync(dto);
        return Ok(ApiResponse<object>.SuccessResponse(new { }));
    }
}
