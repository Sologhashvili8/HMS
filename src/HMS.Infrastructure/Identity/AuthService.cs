using HMS.Application.DTOs.Auth;
using HMS.Application.Exceptions;
using HMS.Application.Interfaces;
using HMS.Application.Interfaces.Services;
using HMS.Domain.Constants;
using HMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace HMS.Infrastructure.Identity;

public class AuthService : IAuthService
{
    private const string VerifyPurpose = "verify-email";
    private const string ResetPurpose = "reset-password";
    private const string ChangePurpose = "change-password";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly ICurrentUserService _currentUser;
    private readonly IEmailService _emailService;
    private readonly IVerificationCodeService _codes;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        ICurrentUserService currentUser,
        IEmailService emailService,
        IVerificationCodeService codes)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _currentUser = currentUser;
        _emailService = emailService;
        _codes = codes;
    }

    public async Task RegisterAsync(RegisterDto dto)
    {
        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
        {
            if (existingUser.EmailConfirmed)
                throw new ConflictException("A user with this email already exists.");

            await SendVerificationCodeAsync(existingUser.Email!);
            return;
        }

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = dto.Email,
            Email = dto.Email,
            EmailConfirmed = false
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, Roles.Guest);

            var guest = new Guest
            {
                Id = user.Id,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PersonalNumber = dto.PersonalNumber,
                PhoneNumber = dto.PhoneNumber
            };

            await _unitOfWork.Repository<Guest>().AddAsync(guest);
            await _unitOfWork.SaveChangesAsync();
        });

        await SendVerificationCodeAsync(user.Email!);
    }

    public async Task<AuthResponseDto> VerifyEmailAsync(VerifyEmailDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new BadRequestException("Invalid or expired code.");

        if (!_codes.Verify(VerifyPurpose, dto.Email, dto.Code))
            throw new BadRequestException("Invalid or expired code.");

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            var update = await _userManager.UpdateAsync(user);
            if (!update.Succeeded)
                throw new BadRequestException(string.Join(" ", update.Errors.Select(e => e.Description)));
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user.Id, user.Email!, roles);

        return new AuthResponseDto { Token = token, Email = user.Email!, Roles = roles };
    }

    public async Task ResendVerificationAsync(ResendVerificationDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user == null || user.EmailConfirmed)
            return;

        await SendVerificationCodeAsync(user.Email!);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new BadRequestException("Invalid email or password.");

        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!passwordValid)
            throw new BadRequestException("Invalid email or password.");

        if (!user.EmailConfirmed)
        {
            await SendVerificationCodeAsync(user.Email!);
            throw new ForbiddenException("Email is not verified. We sent a new code to your email.");
        }

        var roles = await _userManager.GetRolesAsync(user);
        var token = _tokenService.GenerateToken(user.Id, user.Email!, roles);

        return new AuthResponseDto { Token = token, Email = user.Email!, Roles = roles };
    }

    public async Task ForgotPasswordAsync(ForgotPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new NotFoundException("No account found with this email.");

        var code = _codes.Generate(ResetPurpose, user.Email!);
        await _emailService.SendAsync(
            user.Email!,
            "Your BookNRest password reset code",
            BuildCodeEmail("Reset your password", "Use this code to reset your password.", code));
    }

    public async Task ResetPasswordAsync(ResetPasswordDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email)
            ?? throw new BadRequestException("Invalid or expired code.");

        if (!_codes.Verify(ResetPurpose, dto.Email, dto.Code))
            throw new BadRequestException("Invalid or expired code.");

        var identityToken = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, identityToken, dto.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));

        if (!user.EmailConfirmed)
        {
            user.EmailConfirmed = true;
            await _userManager.UpdateAsync(user);
        }
    }

    public async Task SendChangePasswordCodeAsync()
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        var code = _codes.Generate(ChangePurpose, user.Email!);
        await _emailService.SendAsync(
            user.Email!,
            "Your BookNRest password change code",
            BuildCodeEmail("Change your password", "Use this code to confirm your password change.", code));
    }

    public async Task ChangePasswordAsync(ChangePasswordDto dto)
    {
        var userId = _currentUser.UserId ?? throw new ForbiddenException("Not authenticated.");

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found.");

        if (!await _userManager.CheckPasswordAsync(user, dto.CurrentPassword))
            throw new BadRequestException("Current password is incorrect.");

        if (!_codes.Verify(ChangePurpose, user.Email!, dto.Code))
            throw new BadRequestException("Invalid or expired code.");

        var result = await _userManager.ChangePasswordAsync(user, dto.CurrentPassword, dto.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }

    private async Task SendVerificationCodeAsync(string email)
    {
        var code = _codes.Generate(VerifyPurpose, email);
        await _emailService.SendAsync(
            email,
            "Your BookNRest verification code",
            BuildCodeEmail("Verify your email", "Use this code to finish creating your account.", code));
    }

    private static string BuildCodeEmail(string title, string text, string code) =>
        $"<div style=\"font-family:Arial,sans-serif;max-width:480px;margin:auto;padding:24px\">" +
        $"<h2>{title}</h2><p>{text}</p>" +
        $"<p style=\"font-size:32px;font-weight:bold;letter-spacing:6px\">{code}</p>" +
        "<p>The code expires in 10 minutes. If you did not request it, you can ignore this email.</p></div>";
}
