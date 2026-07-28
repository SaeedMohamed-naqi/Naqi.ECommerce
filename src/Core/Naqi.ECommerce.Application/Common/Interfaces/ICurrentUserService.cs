// src/Core/Naqi.ECommerce.Application/Common/Interfaces/ICurrentUserService.cs
//
// Abstraction over "who is making this request" - Application/Domain code
// (and EF Core's SaveChanges override) depend on this, not on
// IHttpContextAccessor directly, keeping HTTP concerns out of those layers.
// Infrastructure implements it using IHttpContextAccessor.

namespace Naqi.ECommerce.Application.Common.Interfaces;

public interface ICurrentUserService
{
    /// <summary>
    /// The logged-in user's Id, or null if there is no authenticated user
    /// (e.g. a background job, the product sync command, or an anonymous request).
    /// </summary>
    Guid? UserId { get; }

    string? UserName { get; }
}