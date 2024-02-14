using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.UseCases.Permissions.Queries;
using ApiService.Domain.Utils;
using ApiService.UnitTests.Setup;
using Microsoft.Extensions.Options;
using Moq;
using Nest;

namespace ApiService.UnitTests.UseCases.Permissions.Queries;

public class GetPermissionsQueryHandlerTests
{
    [Theory]
    [UnitTestsSetup]
    public async Task GetPermissionsQueryHandler_GetData_SuccessfulResponse(
        Mock<IElasticClient> mockElasticClient,
        Mock<IKafkaProducer> mockKafkaProducer,
        Mock<IOptions<KafkaOptions>> mockKafkaOptions
    )
	{
        var handler = new GetPermissionsQueryHandler(
            mockElasticClient.Object,
            mockKafkaProducer.Object,
            mockKafkaOptions.Object
        );
        var ct = new CancellationToken();
        var result = await handler.Handle(new GetPermissionsQuery(), ct);

        mockElasticClient.Verify(m =>
            m.SearchAsync<PermissionRecord>(
                It.IsAny<Func<SearchDescriptor<PermissionRecord>, ISearchRequest>>(),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );
        mockKafkaProducer.Verify(m =>
            m.Produce(
                mockKafkaOptions.Object.Value.PermissionsTopicName,
                It.IsAny<NameOperationEnum>()
            ),
            Times.Exactly(1)
        );
    }
}

