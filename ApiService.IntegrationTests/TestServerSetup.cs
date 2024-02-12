using AutoFixture;
//using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace ApiService.IntegrationTests;

public class TestServerSetup : ICustomization
{
    public void Customize(IFixture fixture)
    {
        var client = new CustomWebApplicationFactory<Program>(fixture).CreateClient();
        fixture.Inject(client);
    }
}