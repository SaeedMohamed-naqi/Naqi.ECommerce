 
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Naqi.ECommerce.Application.Common.Interfaces;

namespace Naqi.ECommerce.Infrastructure.ExternalServices.Sms;

public class TaqnyatSmsSender : ISmsSender
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TaqnyatSmsSender> _logger;

    public TaqnyatSmsSender(HttpClient httpClient, IConfiguration configuration, ILogger<TaqnyatSmsSender> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<SmsSendResult> SendOtpAsync(string phoneNumber, string code, string message, CancellationToken cancellationToken = default)
    {
        var token = _configuration["Taqnyattoken"];
        var link = _configuration["Taqnyatlink"];
        var sender = _configuration["Taqnyatsender"];

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(sender))
        {
            _logger.LogError(
                "Taqnyat SMS config is missing (Taqnyattoken/Taqnyatlink/Taqnyatsender) - cannot send OTP to {Phone}.",
                phoneNumber);
            return SmsSendResult.Fail("SMS provider is not configured.");
        }

        var url = $"{link.TrimEnd('/')}/v1/messages";
        var formattedPhone = FormatPhone(phoneNumber);

        var payload = new
        {
            recipients = new[] { formattedPhone },
            body = message,
            sender
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
        request.Headers.Add("Authorization", $"Bearer {token}");

        HttpResponseMessage response;
        string responseBody;

        try
        {
            response = await _httpClient.SendAsync(request, cancellationToken);
            responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Taqnyat SMS request failed for {Phone} (network/timeout).", phoneNumber);
            return SmsSendResult.Fail("Unable to reach the SMS provider - please try again.");
        }

        // Taqnyat returns statusCode + message in the JSON body even on
        // failure (e.g. {"statusCode":400,"message":"Number(s) is empty
        // or incorrect"}) - that message is exactly what should reach
        // the client, not a generic wrapped exception string.
        TaqnyatResponse? result;
        try
        {
            result = JsonSerializer.Deserialize<TaqnyatResponse>(responseBody,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Taqnyat SMS response was not valid JSON for {Phone}: {ResponseBody}", phoneNumber, responseBody);
            return SmsSendResult.Fail("The SMS provider returned an unexpected response.");
        }

        if (!response.IsSuccessStatusCode || result?.StatusCode != 201)
        {
            _logger.LogError(
                "Taqnyat SMS send failed for {Phone}: StatusCode={StatusCode}, Message={Message}, RawResponse={ResponseBody}",
                phoneNumber, result?.StatusCode, result?.Message, responseBody);

            return SmsSendResult.Fail(
                result?.Message ?? "SMS send failed.",
                result?.StatusCode);
        }

        return SmsSendResult.Ok();
    }

    // Taqnyat expects recipients as plain digits with the country code,
    // no leading 0 or +. Same normalization the legacy FormatPhone helper
    // did for Saudi numbers.
    private static string FormatPhone(string phone)
    {
        return Test_Egypt_FormatPhone(phone);
        //var digits = new string(phone.Where(char.IsDigit).ToArray());

        //if (digits.StartsWith('0'))
        //    digits = digits[1..];

        //if (!digits.StartsWith("966"))
        //    digits = "966" + digits;

        //return digits;
    }
    private static string Test_Egypt_FormatPhone(string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone number is required.", nameof(phone));

        // Keep digits only
        var digits = new string(phone.Where(char.IsDigit).ToArray());

        // 01012345678 -> 201012345678
        if (digits.StartsWith("0"))
            digits = "2" + digits;

        // +201012345678 or 201012345678
        if (digits.StartsWith("20"))
            return digits;

        throw new ArgumentException("Invalid Egyptian phone number.", nameof(phone));
    }
    private class TaqnyatResponse
    {
        public int StatusCode { get; set; }
        public string? Message { get; set; }
    }
}