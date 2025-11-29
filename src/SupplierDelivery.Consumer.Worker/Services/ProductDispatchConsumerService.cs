using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using SupplierDelivery.Consumer.Worker.Options;
using SupplierDelivery.Domain.Contracts;

namespace SupplierDelivery.Consumer.Worker.Services;

public sealed class ProductDispatchConsumerService : BackgroundService
{
    private readonly ILogger<ProductDispatchConsumerService> _logger;
    private readonly KafkaConsumerOptions _options;
    private readonly IProductDispatchProcessor _processor;
    private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IConsumer<string, string> _consumer;

    public ProductDispatchConsumerService(
        ILogger<ProductDispatchConsumerService> logger,
        IOptions<KafkaConsumerOptions> options,
        IProductDispatchProcessor processor)
    {
        _logger = logger;
        _options = options.Value;
        _processor = processor;

        if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers are not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.Topic))
        {
            throw new InvalidOperationException("Kafka topic is not configured.");
        }

        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = string.IsNullOrWhiteSpace(_options.GroupId)
                ? "supplier-delivery-consumer"
                : _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                _logger.LogError("Kafka consumer error: {Reason}", error.Reason);
            })
            .Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_options.Topic);
        _logger.LogInformation("Subscribed to Kafka topic {Topic}", _options.Topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;

            try
            {
                result = _consumer.Consume(stoppingToken);

                if (result?.Message?.Value is null)
                {
                    continue;
                }

                var dispatchMessage = JsonSerializer.Deserialize<ProductDispatchMessage>(
                    result.Message.Value,
                    _serializerOptions);

                if (dispatchMessage is null)
                {
                    _logger.LogWarning("Received message could not be deserialized. Skipping.");
                    continue;
                }

                await _processor.ProcessAsync(dispatchMessage, stoppingToken);
                _consumer.Commit(result);
            }
            catch (ConsumeException consumeException)
            {
                _logger.LogWarning(
                    consumeException,
                    "Kafka consume issue at {TopicPartitionOffset}",
                    result?.TopicPartitionOffset);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Unhandled error while processing Kafka message");
            }
        }
    }

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
