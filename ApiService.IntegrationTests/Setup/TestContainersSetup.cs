using System.Net;
using System.Net.Sockets;
using AutoFixture;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Builders;
using Nito.AsyncEx;

namespace ApiService.IntegrationTests.Setup;

public class TestContainersSetup : ICustomization
{
    private static bool _areRunning;
    private static IContainer _zookeeperContainer;
    private static IContainer _kafkaContainer;

    public void Customize(IFixture fixture)
    {
        if (_areRunning)
        {
            AsyncContext.Run(async () =>
            {
                await _kafkaContainer.StopAsync();
            });
        }
        else
        {
            var zookeeperContainerName = $"zookeeper_{fixture.Create<string>()}";
            _zookeeperContainer = new ContainerBuilder()
                .WithImage("confluentinc/cp-zookeeper:7.0.1")
                .WithName(zookeeperContainerName)
                .WithPortBinding(2181, true)
                .WithEnvironment(new Dictionary<string, string>
                {
                    {"ZOOKEEPER_CLIENT_PORT", "2181"},
                    {"ZOOKEEPER_TICK_TIME", "2000"}
                })
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(2181))
                .Build();

            AsyncContext.Run(async () => await _zookeeperContainer.StartAsync());
        }

        var zookeeperHostPort = _zookeeperContainer.GetMappedPublicPort(2181);

        var kafkaHostPort = GetAvailablePort();
        var kafkaContainerName = $"kafka_{fixture.Create<string>()}";
        _kafkaContainer = new ContainerBuilder()
            .WithImage("confluentinc/cp-kafka:7.0.1")
            .WithName(kafkaContainerName)
            .WithHostname(kafkaContainerName)
            .WithPortBinding(kafkaHostPort, 9092)
            .WithEnvironment(new Dictionary<string, string>
            {
                {"KAFKA_BROKER_ID", "1"},
                {"KAFKA_ZOOKEEPER_CONNECT", $"host.docker.internal:{zookeeperHostPort}"},
                {"KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,PLAINTEXT_INTERNAL:PLAINTEXT"},
                {"KAFKA_LISTENERS", "PLAINTEXT://:9092,PLAINTEXT_INTERNAL://:29092"},
                {"KAFKA_ADVERTISED_LISTENERS", $"PLAINTEXT://localhost:{kafkaHostPort},PLAINTEXT_INTERNAL://{kafkaContainerName}:29092"},
                {"KAFKA_INTER_BROKER_LISTENER_NAME", "PLAINTEXT_INTERNAL"},
                {"KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1"},
                {"KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1"},
                {"KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1"}
            })
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(9092))
            .Build();

        AsyncContext.Run(async () => await _kafkaContainer.StartAsync());

        var topicName = $"orders_{fixture.Create<string>()}";
        var bootstrapServers = $"localhost:{kafkaHostPort}";

        AsyncContext.Run(async () => await CreateKafkaTopic(topicName, bootstrapServers));

        fixture.Inject(new KafkaTestConfig
        {
            TopicName = topicName,
            BootstrapServers = bootstrapServers
        });

        _areRunning = true;
    }

    private static int GetAvailablePort()
    {
        IPEndPoint defaultLoopbackEndpoint = new(IPAddress.Loopback, 0);

        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(defaultLoopbackEndpoint);
        var port = ((IPEndPoint)socket.LocalEndPoint)!.Port;

        return port;
    }

    private static async Task CreateKafkaTopic(string topicName, string bootstrapServers)
    {
        using var adminClient =
            new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();

        await adminClient.CreateTopicsAsync(new TopicSpecification[]
        {
            new() { Name = topicName, ReplicationFactor = 1, NumPartitions = 1 }
        });
    }
}