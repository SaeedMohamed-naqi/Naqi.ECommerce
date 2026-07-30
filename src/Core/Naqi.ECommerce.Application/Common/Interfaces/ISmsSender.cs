 
namespace Naqi.ECommerce.Application.Common.Interfaces;

public record SmsSendResult(bool Success, string? ErrorMessage = null, int? ProviderStatusCode = null)
{
    public static SmsSendResult Ok() => new(true);
    public static SmsSendResult Fail(string errorMessage, int? providerStatusCode = null) => new(false, errorMessage, providerStatusCode);
}

public interface ISmsSender
{
     
    Task<SmsSendResult> SendOtpAsync(string phoneNumber, string code, string message, CancellationToken cancellationToken = default);
}