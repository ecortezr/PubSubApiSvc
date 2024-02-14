using ApiService.Domain.Repositories;
using ApiService.Domain.Utils;
using AutoFixture;
using Microsoft.Extensions.Options;
using Nest;

namespace ApiService.UnitTests.Setup;

public class TestServerSetup : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var apiFactory = new CustomWebApplicationFactory<Program>(fixture);

        fixture.Inject(apiFactory.CreateClient());
        fixture.Inject(apiFactory.GetUnitOfWork());
        fixture.Inject(apiFactory.GetMock<IElasticClient>());
        fixture.Inject(apiFactory.GetMock<IKafkaProducer>());
        fixture.Inject(apiFactory.GetMock<IOptions<KafkaOptions>>());
        //fixture.Inject(apiFactory.GetElasticClient());
    }
}