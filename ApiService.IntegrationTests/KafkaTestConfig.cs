namespace ApiService.IntegrationTests;

public class KafkaTestConfig
{
    public string TopicName { get; set; } = default!;
    public string BootstrapServers { get; set; } = default!;
}