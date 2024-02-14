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

public class CreatePermissionCommandHandlerTests
{
    [Theory]
    [UnitTestsSetup]
    public async Task CreatePermissionCommandHandler_AddData_SuccessfulResponse(
        IUnitOfWork unitOfWork,
        Mock<IKafkaProducer> mockKafkaProducer,
        Mock<IOptions<KafkaOptions>> mockKafkaOptions,
        CreatePermissionCommand command
    )
	{
        var handler = new CreatePermissionCommandHandler(
            unitOfWork,
            mockKafkaProducer.Object,
            mockKafkaOptions.Object
        );
        var result = await handler.Handle(command, new CancellationToken());

        var dbEntry = await unitOfWork.Set<Permission>()
            .FirstOrDefaultAsync(permission =>
                permission.Name == command.Name
            );

        Assert.NotNull(dbEntry);
        mockKafkaProducer.Verify(m =>
            m.Produce(
                mockKafkaOptions.Object.Value.PermissionsTopicName,
                NameOperationEnum.request,
                new PermissionRecord(dbEntry.Id, dbEntry.Name)
            ),
            Times.Exactly(1)
        );
    }
}

