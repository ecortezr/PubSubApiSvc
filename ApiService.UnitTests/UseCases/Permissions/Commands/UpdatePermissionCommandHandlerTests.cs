using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.UseCases.Permissions.Commands;
using ApiService.Domain.Utils;
using ApiService.UnitTests.Setup;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace ApiService.UnitTests.UseCases.Permissions.Commands;

public class UpdatePermissionCommandHandlerTests
{
    [Theory]
    [UnitTestsSetup]
    public async Task UpdatePermissionCommandHandler_UpdateData_SuccessfulResponse(
        IUnitOfWork unitOfWork,
        Mock<IKafkaProducer> mockKafkaProducer,
        Mock<IOptions<KafkaOptions>> mockKafkaOptions,
        UpdateBodyPermissionCommand command,
        string nameSuffix
    )
	{
        // Add a new record
        var createHandler = new CreatePermissionCommandHandler(
            unitOfWork,
            mockKafkaProducer.Object,
            mockKafkaOptions.Object
        );
        var createResult = await createHandler.Handle(
            new CreatePermissionCommand { Name = command.Name },
            new CancellationToken()
        );
        mockKafkaProducer.Verify(m =>
            m.Produce(
                mockKafkaOptions.Object.Value.PermissionsTopicName,
                NameOperationEnum.request,
                createResult
            ),
            Times.Once
        );

        // Update the added record
        var handler = new UpdatePermissionCommandHandler(
            unitOfWork,
            mockKafkaProducer.Object,
            mockKafkaOptions.Object
        );
        var newName = $"{command.Name}-{nameSuffix}";
        var result = await handler.Handle(
            new UpdatePermissionCommand { Id = createResult.Id, Name = newName },
            new CancellationToken()
        );

        var dbEntry = await unitOfWork.Set<Permission>()
            .FirstOrDefaultAsync(permission =>
                permission.Name == newName
            );

        Assert.NotNull(dbEntry);
        Assert.Equal(2, mockKafkaProducer.Invocations.Count);

        mockKafkaProducer.Verify(m =>
            m.Produce(
                mockKafkaOptions.Object.Value.PermissionsTopicName,
                NameOperationEnum.modified,
                dbEntry
            ),
            Times.Exactly(1)
        );
    }
}

