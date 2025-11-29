using SupplierDelivery.Domain.Contracts;

namespace SupplierDelivery.Domain.Entities;

/// <summary>
/// Aggregate root representing the intent to dispatch a product to a supplier endpoint.
/// </summary>
public sealed class ProductDispatch
{
    public Guid DispatchId { get; }
    public string SupplierId { get; }
    public string ProductCode { get; }
    public int Quantity { get; }
    public string TargetEndpoint { get; }
    public DateTimeOffset RequestedAt { get; }
    public string? Notes { get; }

    private ProductDispatch(
        Guid dispatchId,
        string supplierId,
        string productCode,
        int quantity,
        string targetEndpoint,
        DateTimeOffset requestedAt,
        string? notes)
    {
        DispatchId = dispatchId;
        SupplierId = supplierId;
        ProductCode = productCode;
        Quantity = quantity;
        TargetEndpoint = targetEndpoint;
        RequestedAt = requestedAt;
        Notes = notes;
    }

    public static ProductDispatch Create(
        string supplierId,
        string productCode,
        int quantity,
        string targetEndpoint,
        DateTimeOffset? requestedAt = null,
        string? notes = null,
        Guid? dispatchId = null)
    {
        if (string.IsNullOrWhiteSpace(supplierId))
        {
            throw new ArgumentException("SupplierId is required", nameof(supplierId));
        }

        if (string.IsNullOrWhiteSpace(productCode))
        {
            throw new ArgumentException("ProductCode is required", nameof(productCode));
        }

        if (string.IsNullOrWhiteSpace(targetEndpoint))
        {
            throw new ArgumentException("TargetEndpoint is required", nameof(targetEndpoint));
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero");
        }

        var dispatch = new ProductDispatch(
            dispatchId ?? Guid.NewGuid(),
            supplierId.Trim(),
            productCode.Trim(),
            quantity,
            targetEndpoint.Trim(),
            requestedAt ?? DateTimeOffset.UtcNow,
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim());

        return dispatch;
    }

    public ProductDispatchMessage ToMessage() =>
        new(
            DispatchId.ToString(),
            SupplierId,
            ProductCode,
            Quantity,
            RequestedAt,
            TargetEndpoint,
            Notes);
}
