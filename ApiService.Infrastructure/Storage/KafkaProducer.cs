using System.Text.Json;
using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ApiService.Infrastructure.Storage;

public class KafkaProducer : IKafkaProducer
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<Null, string> _kafkaProducer;

    private int disposed;

    public KafkaProducer(
        ILogger<KafkaProducer> logger,
        IOptions<KafkaOptions> kafkaOptions
    )
    {
        _logger = logger;
        var config = new ProducerConfig {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            ClientId = kafkaOptions.Value.ClientId
        };
        _kafkaProducer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task Produce<TMessage>(string topic, TMessage message)
    {
        await _kafkaProducer.ProduceAsync(topic, new Message<Null, string>
        {
            Value = JsonSerializer.Serialize(message)
        });
    }

    public async Task Produce(string topic, NameOperationEnum nameOperation, PermissionRecord? permissionRecord = null)
    {
        var topicMessage = new PermissionTopicMessage(
            Guid.NewGuid(),
            nameOperation,
            permissionRecord
        );
        await Produce<PermissionTopicMessage>(topic, topicMessage);

        _logger.LogInformation($"A new '{nameOperation}' was published on the topic '{topic}'. Message: {topicMessage}");
    }

    public async Task Produce(string topic, NameOperationEnum nameOperation, Permission permission)
    {
        await Produce(topic, nameOperation, new PermissionRecord(permission.Id, permission.Name));
    }

    public async Task Produce(string topic, NameOperationEnum nameOperation)
    {
        await Produce(topic, nameOperation, permissionRecord: null);
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1) return;
        _kafkaProducer?.Flush();
        _kafkaProducer?.Dispose();
    }
}