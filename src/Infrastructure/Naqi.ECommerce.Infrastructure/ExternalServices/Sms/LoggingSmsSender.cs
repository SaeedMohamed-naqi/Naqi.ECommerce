 

using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Infrastructure.ExternalServices.Sms;

public class LoggingSmsSender : ISmsSender
{
    private readonly ILogger<LoggingSmsSender> _logger;

    public LoggingSmsSender(ILogger<LoggingSmsSender> logger) => _logger = logger;

    public Task<SmsSendResult> SendOtpAsync(string phoneNumber, string code, string message, CancellationToken cancellationToken = default)
    {
        // TODO: replace with a real SMS provider call before production.
        _logger.LogWarning(
            "[SMS PLACEHOLDER] Would send to {PhoneNumber}: \"{Message}\" (code: {Code}) - no real SMS provider configured yet.",
            phoneNumber, message, code);

        return Task.FromResult(SmsSendResult.Ok());
    }
}