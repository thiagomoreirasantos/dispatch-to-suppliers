using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SupplierDelivery.Domain.Contracts;
using SupplierDelivery.Producer.Api.Options;

namespace SupplierDelivery.Producer.Api.Services;

public sealed class KafkaProductDispatchProducer : IProductDispatchProducer, IDisposable
{
    private readonly ILogger<KafkaProductDispatchProducer> _logger;
    private readonly KafkaProducerOptions _options;
    private readonly IProducer<string, string> _producer;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
    private bool _disposed;

    public KafkaProductDispatchProducer(
        ILogger<KafkaProductDispatchProducer> logger,
        IOptions<KafkaProducerOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers are not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Topic))
        {
            throw new InvalidOperationException("Kafka topic is not configured.");
        }

        var config = new ProducerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            ClientId = string.IsNullOrWhiteSpace(_options.ClientId)
                ? "supplier-delivery-producer"
                : _options.ClientId
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka producer error: {Reason} (IsFatal={IsFatal})", error.Reason, error.IsFatal);
            })
            .Build();
    }

    public async Task ProduceAsync(ProductDispatchMessage dispatch, CancellationToken cancellationToken)
    {
        var message = new Message<string, string>
        {
            Key = dispatch.DispatchId,
            Value = JsonSerializer.Serialize(dispatch, _serializerOptions)
        };

        _logger.LogInformation(
            "Publishing dispatch {DispatchId} for supplier {SupplierId} to topic {Topic}",
            dispatch.DispatchId,
            dispatch.SupplierId,
            _options.Topic);

        var deliveryResult = await _producer.ProduceAsync(_options.Topic, message, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Dispatch {DispatchId} delivered to {TopicPartitionOffset}",
            dispatch.DispatchId,
            deliveryResult.TopicPartitionOffset);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
        _disposed = true;
    }
}
