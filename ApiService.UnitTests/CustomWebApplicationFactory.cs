using ApiService.Domain.Entities;
using ApiService.Domain.Messages;
using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using ApiService.UnitTests.Setup;
using AutoFixture;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Nest;

namespace ApiService.UnitTests;

public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
{
    private readonly IFixture _fixture;

    public CustomWebApplicationFactory(IFixture fixture)
    {
        _fixture = fixture;
    }

    public IElasticClient GetElasticClient()
    {
        return Services.GetRequiredService<IElasticClient>();
    }

    public IUnitOfWork GetUnitOfWork()
    {
        return Services.GetRequiredService<IUnitOfWork>();
    }

    public Mock<TMock> GetMock<TMock>() where TMock : class
    {
        return Services.GetRequiredService<Mock<TMock>>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("UnitTesting");

        builder.ConfigureServices(services =>
        {
            var kafkaTestConfig = _fixture.Create<KafkaTestConfig>();
            services
                .Configure<KafkaOptions>(opts =>
                {
                    opts.PermissionsTopicName = kafkaTestConfig.TopicName;
                    opts.BootstrapServers = kafkaTestConfig.BootstrapServers;
                    opts.ClientId = kafkaTestConfig.ClientId;
                    opts.GroupId = kafkaTestConfig.GroupId;
                });

            services
                .AddSingleton<Mock<IElasticClient>>(provider => GetElasticSearchClientMock())
                .AddSingleton<Mock<IKafkaProducer>>(provider => GetKafkaProducerMock())
                .AddSingleton<Mock<IOptions<KafkaOptions>>>(provider => GetKafkaOptionsMock(kafkaTestConfig));
        });
    }

    private Mock<IOptions<KafkaOptions>> GetKafkaOptionsMock(KafkaTestConfig kafkaTestConfig)
    {
        var mockKafkaOptions = new Mock<IOptions<KafkaOptions>>();
        mockKafkaOptions
            .Setup(x => x.Value)
            .Returns(new KafkaOptions()
            {
                PermissionsTopicName = kafkaTestConfig.TopicName,
                BootstrapServers = kafkaTestConfig.BootstrapServers,
                ClientId = kafkaTestConfig.ClientId,
                GroupId = kafkaTestConfig.GroupId
            });

        return mockKafkaOptions;
    }

    private Mock<IKafkaProducer> GetKafkaProducerMock()
    {
        var mockKafkaProducer = new Mock<IKafkaProducer>();
        mockKafkaProducer
            .Setup(m =>
                m.Produce(It.IsAny<string>(), It.IsAny<NameOperationEnum>())
            )
            .Returns(Task.FromResult(true));
        mockKafkaProducer
            .Setup(m =>
                m.Produce(It.IsAny<string>(), It.IsAny<NameOperationEnum>(), It.IsAny<PermissionRecord>())
            )
            .Returns(Task.FromResult(true));
        mockKafkaProducer
            .Setup(m =>
                m.Produce(It.IsAny<string>(), It.IsAny<NameOperationEnum>(), It.IsAny<Permission>())
            )
            .Returns(Task.FromResult(true));

        return mockKafkaProducer;
    }

    private Mock<IElasticClient> GetElasticSearchClientMock()
    {
        var permissions = new List<PermissionRecord>
        {
            new PermissionRecord(1, "Permission 1"),
            new PermissionRecord(2, "Permission 2")
        };
        var hits = new List<IHit<PermissionRecord>>
        {
            new Mock<IHit<PermissionRecord>>().Object
        };
        var mockSearchResponse = new Mock<ISearchResponse<PermissionRecord>>();
        mockSearchResponse.Setup(x => x.Documents).Returns(permissions);
        mockSearchResponse.Setup(x => x.Hits).Returns(hits);

        var mockElasticClient = new Mock<IElasticClient>();
        mockElasticClient
            .Setup(y =>
                y.SearchAsync<PermissionRecord>(
                    It.IsAny<Func<SearchDescriptor<PermissionRecord>, ISearchRequest>>(),
                    It.IsAny<CancellationToken>()
                )
                .Result
            )
            .Returns(mockSearchResponse.Object);

        return mockElasticClient;
    }
}