using System.Net.Http.Json;
using SupplierDelivery.Domain.Contracts;

namespace SupplierDelivery.Consumer.Worker.Services;

/// <summary>
/// Sends the consumed dispatch message to the supplier endpoint via HTTP POST.
/// </summary>
public sealed class RestProductDispatchProcessor : IProductDispatchProcessor
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RestProductDispatchProcessor> _logger;

    public RestProductDispatchProcessor(HttpClient httpClient, ILogger<RestProductDispatchProcessor> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task ProcessAsync(ProductDispatchMessage message, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Sending dispatch {DispatchId} to endpoint {Endpoint}",
            message.DispatchId,
            message.TargetEndpoint);

        using var response = await _httpClient.PostAsJsonAsync(
            message.TargetEndpoint,
            message,
            cancellationToken: cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation(
                "Dispatch {DispatchId} delivered to endpoint {Endpoint}",
                message.DispatchId,
                message.TargetEndpoint);
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "Endpoint {Endpoint} returned {StatusCode} for dispatch {DispatchId}. Body: {Body}",
            message.TargetEndpoint,
            response.StatusCode,
            message.DispatchId,
            body);
    }
}
