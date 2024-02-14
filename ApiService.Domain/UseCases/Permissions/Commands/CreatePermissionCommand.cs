using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using MediatR;
using Microsoft.Extensions.Options;

namespace ApiService.Domain.UseCases.Permissions.Commands;

public class CreatePermissionCommand : IRequest<PermissionRecord>
{
    public string Name { get; set; } = default!;
}

public class CreatePermissionCommandHandler : IRequestHandler<CreatePermissionCommand, PermissionRecord>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;

    public CreatePermissionCommandHandler(
        //IConfiguration configuration,
        IUnitOfWork unitOfWork,
        IKafkaProducer kafkaProducer,
        IOptions<KafkaOptions> kafkaOptions
    )
    {
        //_configuration = configuration;
        _unitOfWork = unitOfWork;
        _kafkaProducer = kafkaProducer;
        _kafkaOptions = kafkaOptions;
    }

    public async Task<PermissionRecord> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        var newPermission = new Permission
        {
            Name = request.Name
        };

        _unitOfWork
            .Set<Permission>()
            .Add(newPermission);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var permissionRecord = new PermissionRecord(newPermission.Id, newPermission.Name);
        await ProduceKafkaPermisionMessage(permissionRecord);

        return permissionRecord;
    }

    private async Task ProduceKafkaPermisionMessage(PermissionRecord permission)
    {
        var topicMessage = new PermissionTopicMessage(
            Guid.NewGuid(),
            NameOperationEnum.request,
            permission
        );
        await _kafkaProducer.Produce(
            _kafkaOptions.Value.PermissionsTopicName,
            topicMessage
        );

        Console.WriteLine($"A new 'request' message was published on the topic '{_kafkaOptions.Value.PermissionsTopicName}'. Message: {topicMessage}");
    }
}

