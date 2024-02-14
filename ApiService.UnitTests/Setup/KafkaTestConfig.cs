namespace ApiService.UnitTests.Setup;

public class KafkaTestConfig
{
    public string TopicName { get; set; } = default!;
    public string BootstrapServers { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string GroupId { get; set; } = default!;
}