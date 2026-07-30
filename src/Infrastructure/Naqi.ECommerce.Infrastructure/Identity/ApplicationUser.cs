// src/Infrastructure/Naqi.ECommerce.Infrastructure/Identity/ApplicationUser.cs
//
// Extends IdentityUser<Guid> - Identity keys stay Guid independently of
// Domain entities (BaseEntity.Id is long/bigint). ApplicationRole below
// must use the SAME TKey as ApplicationUser (Guid), since
// IdentityDbContext<TUser, TRole, TKey> requires both to share one key type.
// Lives in Infrastructure, not Domain, because it depends on
// Microsoft.AspNetCore.Identity - Domain must stay framework-free.

using Microsoft.AspNetCore.Identity;

namespace Naqi.ECommerce.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // ---- Phone OTP state ----
    // Mirrors the legacy User.Reset* fields (ResetNewID, ResetExpiry,
    // ResetAttemptCount, ResetSendCount, ResetLastSend, ResetUsed) - same
    // throttling behavior (60s between sends, max 3 sends per 15 min),
    // just renamed from "Reset" (that code originally reused the
    // password-reset code field for OTP too) to "Otp" for clarity, since
    // this is purely a registration/phone-confirmation code now.
    public string? OtpCode { get; set; }
    public DateTime? OtpExpiresAtUtc { get; set; }
    public int OtpAttemptCount { get; set; }
    public int OtpSendCount { get; set; }
    public DateTime? OtpLastSentAtUtc { get; set; }
    public bool OtpUsed { get; set; }

    public ApplicationUser()
    {
        Id = Guid.NewGuid(); // IdentityUser<Guid> doesn't default this itself
    }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base()
    {
        Id = Guid.NewGuid();
    }

    public ApplicationRole(string roleName) : base(roleName)
    {
        Id = Guid.NewGuid();
    }
}

// Well-known role names - reference these everywhere instead of magic strings
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User"; // assigned to both registered and guest API accounts
}