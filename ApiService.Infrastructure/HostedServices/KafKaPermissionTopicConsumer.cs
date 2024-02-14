using System.Text.Json;
using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Utils;
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

                    if (
                        consumedPermission is not null
                        && consumedPermission.NameOperation != NameOperationEnum.get
                    )
                    {
                        Console.WriteLine($"Permission details: {{ Id: {consumedPermission.Permission.Id}, Name: '{consumedPermission.Permission.Name}' }}");

                        var permissionRecord = consumedPermission.Permission;
                        if (consumedPermission.NameOperation == NameOperationEnum.request)
                        {
                            // Add permission to Elastic Search
                            var indexResponse = await _elasticClient
                                .IndexDocumentAsync(
                                    permissionRecord,
                                    stoppingToken
                                );

                            if (indexResponse.IsValid)
                            {
                                Console.WriteLine($"Permission with Id: {permissionRecord.Id} was added to ELK");
                            }
                            else
                            {
                                Console.WriteLine($"debugInfo: {indexResponse.DebugInformation}");
                                Console.WriteLine($"error: {indexResponse.ServerError.Error}");
                            }
                        }
                        else
                        {
                            var updateResponse = await _elasticClient
                                .UpdateAsync<PermissionRecord>(
                                    permissionRecord.Id,
                                    u => u.Doc(permissionRecord),
                                    stoppingToken
                                );
                            if (updateResponse.IsValid)
                            {
                                Console.WriteLine($"Permission with Id: {permissionRecord.Id} was updated on ELK");
                            }
                            else
                            {
                                Console.WriteLine($"debugInfo: {updateResponse.DebugInformation}");
                                Console.WriteLine($"error: {updateResponse.ServerError.Error}");
                            }
                        }
                    }
                }

                consumer.Close();

                Console.WriteLine($"StartConsumerAsync method ended!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex}");
            }
        }
    }
}
