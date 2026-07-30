// src/Presentation/Naqi.ECommerce.Api/Models/RegistrationDtos.cs

namespace Naqi.ECommerce.Api.Models;

public record RegisterRequest(string Phone, string Email, string Password);

public record RegisterResponse(long CustomerId, string Token, bool PhoneConfirmationRequired);

public record ConfirmPhoneRequest(string Phone, string Code);

public record ResendOtpRequest(string Phone);

public record RegisterGuestRequest(string? NameEn = null, string? NameAr = null, string? Email = null, string? Phone = null);

public record RegisterGuestResponse(long CustomerId, Guid GuestToken, string Token);