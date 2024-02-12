using System.Collections.Concurrent;
using System.Text.Json;
using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Infrastructure.Utils;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Nest;

namespace ApiService.Infrastructure.HostedServices
{
    public class KafKaPermissionTopicConsumer : BackgroundService
    {
        private readonly IElasticClient _elasticClient;
        private readonly IOptions<KafkaOptions> _kafkaOptions;

        public KafKaPermissionTopicConsumer(
            IElasticClient elasticClient,
            IOptions<KafkaOptions> kafkaOptions
        )
        {
            _elasticClient = elasticClient;
            _kafkaOptions = kafkaOptions;
        }


        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("KafKa Permission Topic Consumer is starting...");

            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("KafKa Permission Topic Consumer is stopping...");

            await base.StopAsync(cancellationToken);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task.Run(() => StartConsumerAsync(stoppingToken), stoppingToken);
            return Task.CompletedTask;
        }

        private async Task StartConsumerAsync(CancellationToken stoppingToken)
        {
            try
            {
                Console.WriteLine($"StartConsumerAsync method started!");

                var config = new ConsumerConfig
                {
                    BootstrapServers = _kafkaOptions.Value.BootstrapServers,
                    GroupId = Guid.NewGuid().ToString(),
                    AutoOffsetReset = AutoOffsetReset.Earliest
                };
                var consumer = new ConsumerBuilder<Null, string>(config).Build();

                var topicName = _kafkaOptions.Value.PermissionsTopicName;
                consumer.Subscribe(topicName);

                Console.WriteLine($"StartConsumerAsync Consumer subscribe to {topicName}");

                while (!stoppingToken.IsCancellationRequested)
                {
                    var consumeResult = consumer.Consume(stoppingToken);
                    var consumedPermission = JsonSerializer.Deserialize<PermissionTopicMessage>(consumeResult.Message.Value);

                    Console.WriteLine($"Message received from {consumeResult.TopicPartitionOffset}: {consumedPermission}");

                    if (consumedPermission is not null)
                    {
                        Console.WriteLine($"Product details: {{ Id: {consumedPermission.Permission.Id}, Name: '{consumedPermission.Permission.Name}' }}");

                        // Add product to Elastic Search
                        var indexResponse = await _elasticClient
                            .IndexDocumentAsync(consumedPermission.Permission, stoppingToken);
                        if (!indexResponse.IsValid)
                        {
                            Console.WriteLine($"debugInfo: {indexResponse.DebugInformation}");
                            Console.WriteLine($"error: {indexResponse.ServerError.Error}");
                        }
                    }
                }

                Console.WriteLine($"StartConsumerAsync method ended!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }
    }
}
