using AutoFixture;

namespace ApiService.IntegrationTests.Setup;

public class TestServerSetup : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var apiFactory = new CustomWebApplicationFactory<Program>(fixture);

        fixture.Inject(apiFactory.CreateClient());
        fixture.Inject(apiFactory.GetElasticClient());
        fixture.Inject(apiFactory.GetUnitOfWork());
    }
}