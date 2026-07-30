// src/Core/Naqi.ECommerce.Domain/Entities/Customer.cs
//
// Represents EVERY buyer in the system, whether they have a real account
// or checked out as a guest:
//
//   - Registered: ApplicationUserId is set, IsGuest is false.
//   - Guest: ApplicationUserId is null, IsGuest is true.
//
// IMPORTANT architectural note: ApplicationUser (the Identity type) lives
// in Naqi.ECommerce.Infrastructure.Identity, not Domain. Clean
// Architecture's dependency rule says Domain must never reference
// Infrastructure - so this entity deliberately holds only the bare
// Guid? ApplicationUserId scalar, with NO compile-time navigation
// property to ApplicationUser. The actual foreign-key relationship
// (HasOne<ApplicationUser>().WithOne()...) is configured in
// ApplicationDbContext instead, since that class already sits in
// Infrastructure and is allowed to know about both types.

using Naqi.ECommerce.Domain.Common;

namespace Naqi.ECommerce.Domain.Entities;

public class Customer : BaseAuditableEntity
{
    // Null for guest customers. When set, this is the Id of the
    // corresponding ApplicationUser (Identity) row.
    public Guid? ApplicationUserId { get; private set; }

    public bool IsGuest => ApplicationUserId is null;

    // Stable, public-facing identifier - safe to hand to a browser and
    // store in localStorage/a cookie to re-identify a GUEST customer on
    // later requests, without exposing the internal sequential Id (a
    // guessable long) or requiring a full account/JWT. Generated for
    // every customer (registered ones just don't use it, since they
    // authenticate via their real account instead).
    public Guid GuestToken { get; private set; } = Guid.NewGuid();

    public string NameEn { get; private set; } = string.Empty;
    public string? NameAr { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }

    // Mirrors legacy usr.IsComplete - false right after registration
    // (this flow only collects phone/email/password), true once the
    // customer fills in the rest of their profile via a future endpoint.
    // Surfaced to the Next.js frontend as "complete" in the OTP-confirm
    // response, matching what OtpForm.jsx expects from login(token,
    // data.user.complete, ...).
    public bool IsProfileComplete { get; private set; }

    private Customer() { } // EF Core

    private Customer(Guid? applicationUserId, string? nameEn, string? nameAr, string? email, string? phone)
    {
        var (resolvedNameEn, resolvedNameAr) = ResolveNames(nameEn, nameAr);

        ApplicationUserId = applicationUserId;
        NameEn = resolvedNameEn;
        NameAr = resolvedNameAr;
        Email = email;
        Phone = phone;
    }

    public static Customer CreateRegistered(
        Guid applicationUserId, string? nameEn = null, string? nameAr = null, string? email = null, string? phone = null)
    {
        if (applicationUserId == Guid.Empty)
            throw new ArgumentException("A registered customer must have a real ApplicationUserId.", nameof(applicationUserId));

        return new Customer(applicationUserId, nameEn, nameAr, email, phone);
    }

    public static Customer CreateGuest(string? nameEn = null, string? nameAr = null, string? email = null, string? phone = null)
        => new(applicationUserId: null, nameEn, nameAr, email, phone);

    // Converts a guest customer into a registered one - e.g. they create
    // an account sometime after checking out as a guest. Deliberately no
    // "Unlink" method - once tied to a real account, a customer shouldn't
    // be able to go back to being a guest.
    public void LinkToAccount(Guid applicationUserId)
    {
        if (applicationUserId == Guid.Empty)
            throw new ArgumentException("Cannot link to an empty ApplicationUserId.", nameof(applicationUserId));

        if (ApplicationUserId is not null && ApplicationUserId != applicationUserId)
            throw new InvalidOperationException("This customer is already linked to a different account.");

        ApplicationUserId = applicationUserId;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    public void UpdateContactInfo(string? nameEn, string? nameAr, string? email, string? phone)
    {
        var (resolvedNameEn, resolvedNameAr) = ResolveNames(nameEn, nameAr);

        NameEn = resolvedNameEn;
        NameAr = resolvedNameAr;
        Email = email;
        Phone = phone;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    // Not called anywhere yet - a future "complete your profile" endpoint
    // should call this once the customer fills in the fields this
    // minimal phone/email/password registration didn't collect.
    public void MarkProfileComplete()
    {
        IsProfileComplete = true;
        LastModifiedAtUtc = DateTime.UtcNow;
    }

    // If only one language's name was provided, use it for BOTH - a
    // customer shouldn't end up with a populated NameEn and a blank
    // NameAr (or vice versa) just because the registration form only
    // asked for one. Requires at least one to be non-empty; NameEn is
    // the non-nullable "primary" field on this entity, so if only NameAr
    // came in, it becomes both.
    private static (string NameEn, string? NameAr) ResolveNames(string? nameEn, string? nameAr)
    {
        var hasEn = !string.IsNullOrWhiteSpace(nameEn);
        var hasAr = !string.IsNullOrWhiteSpace(nameAr);

        if (!hasEn && !hasAr)
            throw new ArgumentException("At least one of NameEn or NameAr must be provided.");

        var resolvedEn = hasEn ? nameEn! : nameAr!;
        var resolvedAr = hasAr ? nameAr! : nameEn!;

        return (resolvedEn, resolvedAr);
    }
}