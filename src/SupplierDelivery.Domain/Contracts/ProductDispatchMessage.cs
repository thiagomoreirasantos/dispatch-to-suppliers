namespace SupplierDelivery.Domain.Contracts;

/// <summary>
/// Canonical contract for dispatch events sent over Kafka.
/// </summary>
/// <param name="DispatchId">Unique identifier for the dispatch.</param>
/// <param name="SupplierId">Supplier identifier that will receive the product.</param>
/// <param name="ProductCode">Internal or catalog code of the product being sent.</param>
/// <param name="Quantity">Quantity to deliver.</param>
/// <param name="RequestedAt">Timestamp when the dispatch was requested.</param>
/// <param name="TargetEndpoint">Endpoint that will receive the dispatch payload on the consumer side.</param>
/// <param name="Notes">Optional free text to give context.</param>
public sealed record ProductDispatchMessage(
    string DispatchId,
    string SupplierId,
    string ProductCode,
    int Quantity,
    DateTimeOffset RequestedAt,
    string TargetEndpoint,
    string? Notes);
