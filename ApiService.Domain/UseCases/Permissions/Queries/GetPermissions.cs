using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using MediatR;
using Microsoft.Extensions.Options;

namespace ApiService.Domain.UseCases.Permissions.Queries;

public class GetPermissionsQuery : IRequest<List<GetPermissionsQueryResponse>>
{
}

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<GetPermissionsQueryResponse>>
{
    private readonly Nest.IElasticClient _elasticClient;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;

    public GetPermissionsQueryHandler(
        Nest.IElasticClient elasticClient,
        IKafkaProducer kafkaProducer,
        IOptions<KafkaOptions> kafkaOptions
    )
    {
        _elasticClient = elasticClient;
        _kafkaProducer = kafkaProducer;
        _kafkaOptions = kafkaOptions;
    }

    public async Task<List<GetPermissionsQueryResponse>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var searchResponse = await _elasticClient
            .SearchAsync<PermissionRecord>(
                s => s.MatchAll(),
                cancellationToken
            );

        await ProduceKafkaPermisionMessage();

        return searchResponse.Documents
            .Select(p =>
                new GetPermissionsQueryResponse(p.Id, p.Name)
            )
            .ToList();
    }

    private async Task ProduceKafkaPermisionMessage()
    {
        var topicMessage = new PermissionTopicMessage(
            Guid.NewGuid(),
            NameOperationEnum.get,
            null
        );
        await _kafkaProducer.Produce(
            _kafkaOptions.Value.PermissionsTopicName,
            topicMessage
        );

        Console.WriteLine($"A new 'get' message was published on the topic '{_kafkaOptions.Value.PermissionsTopicName}'. Message: {topicMessage}");
    }
}

public record GetPermissionsQueryResponse(
    int Id,
    string Name
);
