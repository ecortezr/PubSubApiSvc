namespace ApiService.Domain.Repositories;

public interface IKafkaConsumer : IDisposable
{
    public void Subscribe(string topic);

    public Task ConsumePermissionTopicMessage(CancellationToken stoppingToken);
}
