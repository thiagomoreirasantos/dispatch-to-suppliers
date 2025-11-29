using SupplierDelivery.Domain.Contracts;

namespace SupplierDelivery.Consumer.Worker.Services;

public interface IProductDispatchProcessor
{
    Task ProcessAsync(ProductDispatchMessage message, CancellationToken cancellationToken);
}
