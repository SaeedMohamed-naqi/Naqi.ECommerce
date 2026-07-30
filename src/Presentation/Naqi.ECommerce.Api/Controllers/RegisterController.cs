// src/Presentation/Naqi.ECommerce.Api/Controllers/RegisterController.cs
//
// Localized the same way the Dashboard is: IStringLocalizer<SharedResource>
// backed by the JSON resource files (see LocalizationDependencyInjection),
// instead of hardcoded Arabic string literals. IMPORTANT: this only
// resolves correctly if the Api project's Program.cs also calls
// AddNaqiLocalization(...) and app.UseRequestLocalization(...) - the same
// two calls the Dashboard's Program.cs already makes. If the Api project
// was scaffolded before that localization work existed, those two calls
// need to be added there too, or IStringLocalizer<SharedResource> will
// just echo back the key names instead of actual translated text.
//
// OTP handling ported from the legacy Register action's Reset* field
// pattern (renamed Otp* here since it's purely a phone-confirmation code
// now, not shared with password reset): a 6-digit code, 10-minute
// expiry, a 60-second cooldown between sends, and a max of 3 sends per
// 15-minute window. Actual delivery goes through ISmsSender
// (TaqnyatSmsSender - see that file for the Taqnyat API integration
// ported from the legacy inline HttpClient call).
//
// Response shapes intentionally match what the existing Next.js frontend
// (OtpForm.jsx) already expects - it checks `data.success`/`data.message`
// rather than HTTP status codes, and reads `data.retryAfter` to drive the
// resend cooldown UI, and `data.token`/`data.user.{complete,email,phone}`
// on a successful confirm. So this controller returns 200 OK with a
// `success: false` body for expected business-rule failures (wrong code,
// throttled resend, etc.), matching the legacy convention the frontend
// was already built against.

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Naqi.ECommerce.Api.Models;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Application.Resources;
using Naqi.ECommerce.Domain.Entities;
using Naqi.ECommerce.Infrastructure.Identity;

namespace Naqi.ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RegisterController : ControllerBase
{
    private const int OtpExpiryMinutes = 10;
    private const int ResendCooldownSeconds = 60;
    private const int MaxSendsPerWindow = 3;
    private static readonly TimeSpan SendWindow = TimeSpan.FromMinutes(15);
    private const int MaxVerifyAttempts = 5;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly ISmsSender _smsSender;
    private readonly IJwtTokenGenerator _tokenGenerator;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public RegisterController(
        UserManager<ApplicationUser> userManager,
        IApplicationDbContext context,
        ISmsSender smsSender,
        IJwtTokenGenerator tokenGenerator,
        IStringLocalizer<SharedResource> localizer)
    {
        _userManager = userManager;
        _context = context;
        _smsSender = smsSender;
        _tokenGenerator = tokenGenerator;
        _localizer = localizer;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return Ok(new { success = false, message = _localizer["RegistrationFieldsRequired"].Value });

        var existingByPhone = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.Phone);
        var existingByEmail = await _userManager.FindByEmailAsync(request.Email);

        if (existingByPhone is not null || existingByEmail is not null)
            return Ok(new { success = false, message = _localizer["AccountAlreadyExists"].Value });

        var user = new ApplicationUser
        {
            UserName = request.Email, // Identity requires a UserName - email doubles as one since this flow has no separate username field
            Email = request.Email,
            PhoneNumber = request.Phone,
            PhoneNumberConfirmed = false,
            EmailConfirmed = false
        };

        var createResult = await _userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            return Ok(new
            {
                success = false,
                message = _localizer["RegistrationFailed"].Value,
                errors = createResult.Errors.Select(e => e.Description) // Identity's own errors - not ours to localize here
            });
        }

        await _userManager.AddToRoleAsync(user, Roles.User);

        // Placeholder name until the customer fills in a real profile
        // later - this registration flow deliberately only collects
        // phone/email/password. IsProfileComplete stays false.
        var customer = Customer.CreateRegistered(user.Id, nameEn: request.Email, email: request.Email, phone: request.Phone);
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(CancellationToken.None);

        var otpResult = await TryIssueOtpAsync(user, CancellationToken.None);
        if (!otpResult.Success)
        {
            // Registration itself succeeded even if this particular OTP
            // send got throttled (shouldn't normally happen on a brand
            // new user, but stay consistent with the resend endpoint's
            // contract just in case).
            return Ok(new
            {
                success = true,
                customerId = customer.Id,
                message = otpResult.ErrorMessage,
                retryAfter = otpResult.RetryAfterSeconds,
                phoneConfirmationRequired = true
            });
        }

        return Ok(new
        {
            success = true,
            customerId = customer.Id,
            message = _localizer["RegistrationSuccessOtpSent"].Value,
            phoneConfirmationRequired = true
        });
    }

    // Matches the Next.js frontend's /api/acceptotp contract.
    [HttpPost("confirm-phone")]
    public async Task<IActionResult> ConfirmPhone([FromBody] ConfirmPhoneRequest request)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.Phone);
        if (user is null)
            return Ok(new { success = false, message = _localizer["UserNotFound"].Value });

        if (!user.PhoneNumberConfirmed)
        {
            if (user.OtpUsed || user.OtpCode is null || user.OtpExpiresAtUtc is null || DateTime.UtcNow > user.OtpExpiresAtUtc)
                return Ok(new { success = false, message = _localizer["OtpExpired"].Value });

            if (user.OtpAttemptCount >= MaxVerifyAttempts)
                return Ok(new { success = false, message = _localizer["OtpMaxAttemptsExceeded"].Value });

            if (user.OtpCode != request.Code)
            {
                user.OtpAttemptCount++;
                await _userManager.UpdateAsync(user);
                return Ok(new { success = false, message = _localizer["OtpIncorrect"].Value });
            }

            user.PhoneNumberConfirmed = true;
            user.OtpUsed = true;
            user.OtpCode = null;
            await _userManager.UpdateAsync(user);
        }

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.ApplicationUserId == user.Id);

        var token = _tokenGenerator.GenerateToken(new JwtTokenRequest(
            Subject: user.Id.ToString(),
            CustomerId: customer?.Id ?? 0,
            Role: Roles.User,
            IsGuest: false,
            Email: user.Email,
            Phone: user.PhoneNumber,
            PhoneConfirmed: true));

        return Ok(new
        {
            success = true,
            message = _localizer["OtpVerifiedSuccess"].Value,
            token,
            user = new
            {
                complete = customer?.IsProfileComplete ?? false,
                email = user.Email,
                phone = user.PhoneNumber
            }
        });
    }

    // Matches the Next.js frontend's reuse of /api/forgotpassword for
    // resending the registration OTP - same throttling contract
    // (data.success / data.message / data.retryAfter).
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == request.Phone);
        if (user is null)
            return Ok(new { success = false, message = _localizer["PhoneNotFound"].Value });

        if (user.PhoneNumberConfirmed)
            return Ok(new { success = false, message = _localizer["PhoneAlreadyConfirmed"].Value });

        var result = await TryIssueOtpAsync(user, CancellationToken.None);
        if (!result.Success)
            return Ok(new { success = false, message = result.ErrorMessage, retryAfter = result.RetryAfterSeconds });

        return Ok(new { success = true, message = _localizer["OtpResentSuccess"].Value });
    }

    private record OtpIssueResult(bool Success, string? ErrorMessage, int? RetryAfterSeconds);
 
    private async Task<OtpIssueResult> TryIssueOtpAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        if (user.OtpLastSentAtUtc.HasValue && now < user.OtpLastSentAtUtc.Value.AddSeconds(ResendCooldownSeconds))
        {
            var retryAfter = (int)Math.Ceiling((user.OtpLastSentAtUtc.Value.AddSeconds(ResendCooldownSeconds) - now).TotalSeconds);
            return new OtpIssueResult(false, _localizer["OtpResendCooldown", ResendCooldownSeconds].Value, retryAfter);
        }

        var withinWindow = user.OtpLastSentAtUtc.HasValue && now < user.OtpLastSentAtUtc.Value.Add(SendWindow);

        if (withinWindow && user.OtpSendCount >= MaxSendsPerWindow)
        {
            var retryAfter = (int)Math.Ceiling((user.OtpLastSentAtUtc!.Value.Add(SendWindow) - now).TotalSeconds);
            return new OtpIssueResult(false, _localizer["OtpResendLimitExceeded"].Value, retryAfter);
        }

        var code = Random.Shared.Next(100000, 999999).ToString();

        user.OtpCode = code;
        user.OtpExpiresAtUtc = now.AddMinutes(OtpExpiryMinutes);
        user.OtpAttemptCount = 0;
        user.OtpUsed = false;
        user.OtpSendCount = withinWindow ? user.OtpSendCount + 1 : 1;
        user.OtpLastSentAtUtc = now;

        await _userManager.UpdateAsync(user);

        var smsMessage = _localizer["OtpSmsMessage", code].Value;
        var smsResult = await _smsSender.SendOtpAsync(user.PhoneNumber!, code, smsMessage, cancellationToken);

        if (!smsResult.Success)
        {
            // OTP state above is already saved, so a resend will still
            // respect the cooldown/window correctly - only the actual
            // delivery failed. The provider's own message ("Number(s) is
            // empty or incorrect", etc.) is exactly what should reach the
            // client here, not a generic wrapped exception.
            return new OtpIssueResult(false, smsResult.ErrorMessage ?? _localizer["OtpSendFailed"].Value, null);
        }

        return new OtpIssueResult(true, null, null);
    }
}