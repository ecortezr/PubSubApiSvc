using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiService.Domain.UseCases.Permissions.Commands;

public class UpdateBodyPermissionCommand : IRequest<PermissionRecord>
{
    public string Name { get; set; } = default!;
}

public class UpdatePermissionCommand : UpdateBodyPermissionCommand
{
    public int Id { get; set; }
}


public class UpdatePermissionCommandHandler : IRequestHandler<UpdatePermissionCommand, PermissionRecord?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IOptions<KafkaOptions> _kafkaOptions;

    public UpdatePermissionCommandHandler(
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

    public async Task<PermissionRecord?> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var permission = await _unitOfWork.Set<Permission>()
            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (permission is null)
        {
            return null;
        }

        if (request.Name != permission.Name)
        {
            permission.Name = request.Name;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await ProduceKafkaPermisionMessage(permission);
        }
        var permissionRecord = new PermissionRecord(permission.Id, permission.Name);

        return permissionRecord;
    }

    private async Task ProduceKafkaPermisionMessage(Permission permission)
    {
        var topicMessage = new PermissionTopicMessage(
            Guid.NewGuid(),
            NameOperationEnum.modified,
            new PermissionRecord(permission.Id, permission.Name)
        );
        await _kafkaProducer.Produce(
            _kafkaOptions.Value.PermissionsTopicName,
            topicMessage
        );

        Console.WriteLine($"A new 'modified' message was published on the topic '{_kafkaOptions.Value.PermissionsTopicName}'. Message: {topicMessage}");
    }
}

