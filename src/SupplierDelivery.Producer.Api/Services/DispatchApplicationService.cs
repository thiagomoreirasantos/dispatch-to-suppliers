using System.ComponentModel.DataAnnotations;
using SupplierDelivery.Domain.Entities;
using SupplierDelivery.Domain.Contracts;
using SupplierDelivery.Producer.Api.Contracts;

namespace SupplierDelivery.Producer.Api.Services;

public sealed class DispatchApplicationService
{
    private readonly IProductDispatchProducer _producer;
    private readonly ILogger<DispatchApplicationService> _logger;

    public DispatchApplicationService(
        IProductDispatchProducer producer,
        ILogger<DispatchApplicationService> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task<ProductDispatchMessage> DispatchAsync(CreateDispatchRequest request, CancellationToken cancellationToken)
    {
        Validate(request);

        var dispatch = ProductDispatch.Create(
            request.SupplierId,
            request.ProductCode,
            request.Quantity,
            request.TargetEndpoint,
            requestedAt: DateTimeOffset.UtcNow,
            notes: request.Notes);

        var message = dispatch.ToMessage();

        await _producer.ProduceAsync(message, cancellationToken);

        _logger.LogInformation(
            "Dispatch request {DispatchId} queued successfully",
            message.DispatchId);

        return message;
    }

    private static void Validate(CreateDispatchRequest request)
    {
        var context = new ValidationContext(request);
        Validator.ValidateObject(request, context, validateAllProperties: true);
    }
}
