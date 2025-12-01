using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SupplierDelivery.Domain.Contracts;
using SupplierDelivery.Producer.Api.Options;

namespace SupplierDelivery.Producer.Api.Services;

public sealed class KafkaSynchronousProductDispatchProducer : IProductDispatchProducer, IDisposable
{
    private readonly ILogger<KafkaSynchronousProductDispatchProducer> _logger;
    private readonly KafkaProducerOptions _options;
    private readonly IProducer<string, string> _producer;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
    private bool _disposed;

    public KafkaSynchronousProductDispatchProducer(
        ILogger<KafkaSynchronousProductDispatchProducer> logger,
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
                _logger.LogError(
                    "Kafka producer error (sync): {Reason} (IsFatal={IsFatal})",
                    error.Reason,
                    error.IsFatal);
            })
            .Build();
    }

    public Task ProduceAsync(ProductDispatchMessage dispatch, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var processorCount = Environment.ProcessorCount;

        Parallel.ForEach(new[] { dispatch }, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = processorCount
        }, item =>
        {
            var message = new Message<string, string>
            {
                Key = item.DispatchId,
                Value = JsonSerializer.Serialize(item, _serializerOptions)
            };

            _logger.LogInformation(
                "Publishing dispatch {DispatchId} for supplier {SupplierId} to topic {Topic} (sync)",
                item.DispatchId,
                item.SupplierId,
                _options.Topic);

            try
            {
                _producer.Produce(_options.Topic, message);

                _logger.LogInformation(
                    "Dispatch {DispatchId} delivered to topic {Topic} (sync)",
                    item.DispatchId,
                    _options.Topic);
            }
            catch (ProduceException<string, string> exception)
            {
                _logger.LogError(
                    exception,
                    "Failed to deliver dispatch {DispatchId} to topic {Topic} (sync)",
                    item.DispatchId,
                    _options.Topic);
                throw;
            }
        });

        return Task.CompletedTask;
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
