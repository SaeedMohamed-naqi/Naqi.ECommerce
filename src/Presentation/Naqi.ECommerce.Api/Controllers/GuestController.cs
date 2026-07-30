// src/Presentation/Naqi.ECommerce.Api/Controllers/GuestController.cs
//
// Guests get NO ApplicationUser row at all - no password, no Identity
// account, nothing in AspNetUsers. Just a Customer record
// (Customer.CreateGuest, ApplicationUserId stays null) and a JWT so they
// can place orders through the same authenticated endpoints a registered
// user would use. The JWT's Subject is a synthetic "guest:{customerId}"
// identifier rather than a real Identity user id, since there's no
// ApplicationUser to reference - but the Role claim is still "User", so
// [Authorize(Roles = "User")] endpoints work identically for both.

using Microsoft.AspNetCore.Mvc;
using Naqi.ECommerce.Api.Models;
using Naqi.ECommerce.Application.Common.Interfaces;
using Naqi.ECommerce.Domain.Entities;
using Naqi.ECommerce.Infrastructure.Identity;

namespace Naqi.ECommerce.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GuestController : ControllerBase
{
    private readonly IApplicationDbContext _context;
    private readonly IJwtTokenGenerator _tokenGenerator;

    public GuestController(IApplicationDbContext context, IJwtTokenGenerator tokenGenerator)
    {
        _context = context;
        _tokenGenerator = tokenGenerator;
    }

    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterGuestRequest request)
    {
        var hasAnyName = !string.IsNullOrWhiteSpace(request.NameEn) || !string.IsNullOrWhiteSpace(request.NameAr);

        // Customer.CreateGuest requires at least one language's name -
        // a true walk-in guest might not provide either, so default to a
        // generic placeholder here rather than making the entity itself
        // lenient about that (its invariant - "at least one name" -
        // stays meaningful for every other caller).
        var customer = hasAnyName
            ? Customer.CreateGuest(request.NameEn, request.NameAr, request.Email, request.Phone)
            : Customer.CreateGuest("Guest", "ضيف", request.Email, request.Phone);

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(CancellationToken.None);

        var token = _tokenGenerator.GenerateToken(new JwtTokenRequest(
            Subject: customer.GuestToken.ToString(),
            CustomerId: customer.Id,
            Role: Roles.User,
            IsGuest: true,
            GuestToken: customer.GuestToken,
            Email: request.Email,
            Phone: request.Phone,
            PhoneConfirmed: false));

        return Ok(new RegisterGuestResponse(customer.Id, customer.GuestToken, token));
    }
}