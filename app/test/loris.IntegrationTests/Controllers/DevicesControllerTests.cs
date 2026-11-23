namespace loris.IntegrationTests.Controllers;

public class DevicesControllerTests(
    TimescaleDbApplicationFactory<Program> timescaleDbApplicationFactory
) : IClassFixture<TimescaleDbApplicationFactory<Program>>
{
    private readonly TimescaleDbApplicationFactory<Program> _appFactory =
        timescaleDbApplicationFactory;

    [Fact]
    public async Task Test1()
    {
        var client = _appFactory.CreateClient();

        var response = await client.GetAsync("/api/testing/test");

        response.EnsureSuccessStatusCode();
    }
}
