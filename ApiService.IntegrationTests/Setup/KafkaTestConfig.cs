namespace ApiService.IntegrationTests.Setup;

public class KafkaTestConfig
{
    public string TopicName { get; set; } = default!;
    public string BootstrapServers { get; set; } = default!;
}