namespace ApiService.Domain.Utils
{
    public class KafkaOptions
    {
        public string BootstrapServers { get; set; } = default!;
        public string PermissionsTopicName { get; set; } = default!;
    }
}