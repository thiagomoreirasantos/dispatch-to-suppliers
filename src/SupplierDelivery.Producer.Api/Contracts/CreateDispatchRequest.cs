using System.ComponentModel.DataAnnotations;

namespace SupplierDelivery.Producer.Api.Contracts;

public sealed record CreateDispatchRequest(
    [property: Required] string SupplierId,
    [property: Required] string ProductCode,
    [property: Range(1, int.MaxValue)] int Quantity,
    [property: Required, Url] string TargetEndpoint,
    string? Notes);
