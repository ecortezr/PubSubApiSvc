using AutoFixture;
using AutoFixture.Xunit2;

namespace ApiService.UnitTests.Setup;

public class UnitTestsSetup : AutoDataAttribute
{
    public UnitTestsSetup() : base(() => new Fixture()
        .Customize(new TestServerSetup()))
    {
    }
}