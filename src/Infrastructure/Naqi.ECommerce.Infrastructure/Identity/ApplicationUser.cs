// src/Infrastructure/Naqi.ECommerce.Infrastructure/Identity/ApplicationUser.cs
//
// Extends IdentityUser<int> (int keys are simpler to join against your
// existing Customers/Orders tables than the default Guid/string keys).
// Lives in Infrastructure, not Domain, because it depends on
// Microsoft.AspNetCore.Identity - Domain must stay framework-free.

using Microsoft.AspNetCore.Identity;

namespace Naqi.ECommerce.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}

// Well-known role names - reference these everywhere instead of magic strings
public static class Roles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
}