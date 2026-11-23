namespace loris.IntegrationTests.Controllers;

public class AutomationsControllerTests(
    TimescaleDbApplicationFactory<Program> timescaleDbApplicationFactory
) : IClassFixture<TimescaleDbApplicationFactory<Program>>
{
    private readonly TimescaleDbApplicationFactory<Program> _timescaleDbApplicationFactory =
        timescaleDbApplicationFactory;

    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}
