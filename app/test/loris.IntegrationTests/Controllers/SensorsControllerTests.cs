namespace loris.IntegrationTests.Controllers;

public class SensorsControllerTests(TimescaleDbApplicationFactory timescaleDbApplicationFactory)
    : IClassFixture<TimescaleDbApplicationFactory>
{
    private readonly TimescaleDbApplicationFactory _timescaleDbApplicationFactory =
        timescaleDbApplicationFactory;

    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}
