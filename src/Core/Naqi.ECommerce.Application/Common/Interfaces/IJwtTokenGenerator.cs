// src/Core/Naqi.ECommerce.Application/Common/Interfaces/IJwtTokenGenerator.cs
//
// Deliberately takes plain claims data (not ApplicationUser) so it works
// for BOTH flows: a real registered user (userId = their real
// ApplicationUser.Id) and a guest (userId = a synthetic identifier tied
// to their Customer record - no ApplicationUser row exists for guests at
// all). Both end up with a "User" role claim, so downstream
// [Authorize(Roles = "User")] endpoints treat them identically; the
// IsGuest claim lets specific endpoints special-case guests if needed
// (e.g. "must be a real account to view saved addresses").

namespace Naqi.ECommerce.Application.Common.Interfaces;

public record JwtTokenRequest(
    string Subject,       // ApplicationUser.Id.ToString() for real users, Customer.GuestToken.ToString() for guests
    long CustomerId,
    string Role,
    bool IsGuest,
    Guid? GuestToken = null,
    string? Email = null,
    string? Phone = null,
    bool PhoneConfirmed = false);

public interface IJwtTokenGenerator
{
    string GenerateToken(JwtTokenRequest request);
}