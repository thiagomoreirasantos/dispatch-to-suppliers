using SupplierDelivery.Domain.Contracts;

namespace SupplierDelivery.Producer.Api.Services;

public interface IProductDispatchProducer
{
    Task ProduceAsync(ProductDispatchMessage dispatch, CancellationToken cancellationToken);
}
