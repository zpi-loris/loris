using Loris.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace loris.IntegrationTests;

public sealed class TimescaleDbApplicationFactory<TProgram>
    : WebApplicationFactory<TProgram>,
        IAsyncLifetime
    where TProgram : class
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder(
        "timescale/timescaledb:latest-pg18"
    ).Build();

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        await ctx.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.ConfigureDbContext<LorisDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString())
            );

            var hostedServiceDescriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }
        });
    }
}
