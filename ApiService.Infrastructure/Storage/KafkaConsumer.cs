using System.Text.Json;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;

namespace ApiService.Infrastructure.Storage;

public class KafkaConsumer : IKafkaConsumer
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IElasticClient _elasticClient;
    private readonly IConsumer<Null, string> _kafkaConsumer;

    private int disposed;

    public KafkaConsumer(
        ILogger<KafkaProducer> logger,
        IElasticClient elasticClient,
        IOptions<KafkaOptions> kafkaOptions
    )
    {
        _logger = logger;
        _elasticClient = elasticClient;

        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaOptions.Value.BootstrapServers,
            ClientId = kafkaOptions.Value.ClientId,
            GroupId = kafkaOptions.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest
        };
        _kafkaConsumer = new ConsumerBuilder<Null, string>(config)
            .Build();

        Subscribe(kafkaOptions.Value.PermissionsTopicName);
    }

    public void Subscribe(string topic)
    {
        _kafkaConsumer.Subscribe(topic);
        _logger.LogInformation($"Consumer subscribe to {topic}");
    }

    public async Task ConsumePermissionTopicMessage(CancellationToken stoppingToken)
    {
        var consumeResult = _kafkaConsumer.Consume(stoppingToken);
        var consumedPermission = JsonSerializer.Deserialize<PermissionTopicMessage>(consumeResult.Message.Value);
        _logger.LogInformation($"Message received from {consumeResult.TopicPartitionOffset}: {consumedPermission}");

        if (
            consumedPermission is null
            || consumedPermission.NameOperation == NameOperationEnum.get
            || consumedPermission.Permission is null
        )
        {
            return;
        }

        var permissionRecord = consumedPermission.Permission;
        _logger.LogInformation($"Permission details: {{ Id: {consumedPermission.Permission.Id}, Name: '{consumedPermission.Permission.Name}' }}");

        switch (consumedPermission.NameOperation)
        {
            case NameOperationEnum.request:
                await AddEntry(permissionRecord, stoppingToken);
                break;

            case NameOperationEnum.modified:
                await UpdateEntry(permissionRecord, stoppingToken);
                break;

            default:
                break;
        }
    }

    private async Task AddEntry(PermissionRecord permissionRecord, CancellationToken stoppingToken)
    {
        var indexResponse = await _elasticClient
            .IndexDocumentAsync(
                permissionRecord,
                stoppingToken
            );

        if (indexResponse.IsValid)
        {
            _logger.LogInformation($"Permission with Id: {permissionRecord.Id} was added to Elasticsearch");
        }
        else
        {
            LogDebug(indexResponse.DebugInformation, indexResponse.ServerError.Error);
        }
    }

    private async Task UpdateEntry(PermissionRecord permissionRecord, CancellationToken stoppingToken)
    {
        var updateResponse = await _elasticClient
            .UpdateAsync<PermissionRecord>(
                permissionRecord.Id,
                u => u.Doc(permissionRecord),
                stoppingToken
            );

        if (updateResponse.IsValid)
        {
            _logger.LogInformation($"Permission with Id: {permissionRecord.Id} was updated on Elasticsearch");
        }
        else
        {
            LogDebug(updateResponse.DebugInformation, updateResponse.ServerError.Error);
        }
    }

    private void LogDebug(string debugInformation, Elasticsearch.Net.Error errorMessage)
    {
        _logger.LogDebug($"Invalid response from Elasticsearch");
        _logger.LogDebug($"debugInfo: {debugInformation}");
        _logger.LogDebug($"error: {errorMessage}");
    }

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref disposed, 1, 0) == 1) return;
        _kafkaConsumer?.Close();
        _kafkaConsumer?.Dispose();
    }
}