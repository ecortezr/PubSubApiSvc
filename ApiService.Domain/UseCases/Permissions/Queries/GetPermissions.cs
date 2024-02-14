using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using MediatR;
using Microsoft.Extensions.Options;

namespace ApiService.Domain.UseCases.Permissions.Queries;

public class GetPermissionsQuery : IRequest<List<PermissionRecord>>
{
}

public class GetPermissionsQueryHandler : IRequestHandler<GetPermissionsQuery, List<PermissionRecord>>
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

    public async Task<List<PermissionRecord>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var searchResponse = await _elasticClient
            .SearchAsync<PermissionRecord>(
                s => s.MatchAll(),
                cancellationToken
            );

        await _kafkaProducer.Produce(
            _kafkaOptions.Value.PermissionsTopicName,
            NameOperationEnum.get
        );

        return searchResponse.Documents
            .ToList();
    }
}
