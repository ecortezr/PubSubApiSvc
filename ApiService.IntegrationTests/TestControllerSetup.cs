using AutoFixture;
using AutoFixture.Xunit2;

namespace ApiService.IntegrationTests;

public class TestControllerSetup : AutoDataAttribute
{
    public TestControllerSetup() : base(() => new Fixture()
        .Customize(new TestContainersSetup())
        .Customize(new KafkaConsumerSetup())
        .Customize(new TestServerSetup()))
    {
    }
}