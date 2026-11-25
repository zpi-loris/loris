namespace loris.IntegrationTests.Controllers;

[Collection(TimescaleDbApplicationFactory.CollectionKey)]
public class SensorsControllerTests(TimescaleDbApplicationFactory timescaleDbApplicationFactory)
{
    private readonly TimescaleDbApplicationFactory _appFactory = timescaleDbApplicationFactory;

    [Fact]
    public void Test1()
    {
        Assert.True(true);
    }
}
