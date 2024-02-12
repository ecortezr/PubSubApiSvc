namespace ApiService.Domain.Repositories;

public interface IKafkaProducer : IDisposable
{
    public Task Produce<TMessage>(string topic, TMessage message);
}
