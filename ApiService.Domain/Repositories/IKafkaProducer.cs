using ApiService.Domain.Entities;
using ApiService.Domain.Messages;

namespace ApiService.Domain.Repositories;

public interface IKafkaProducer : IDisposable
{
    public Task Produce<TMessage>(string topic, TMessage message);

    public Task Produce(string topic, NameOperationEnum nameOperation, Permission permission);

    public Task Produce(string topic, NameOperationEnum nameOperation, PermissionRecord permission);

    public Task Produce(string topic, NameOperationEnum nameOperation);
}
