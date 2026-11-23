using Loris.Data;
using Loris.Jobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace loris.IntegrationTests;

public sealed class TimescaleDbApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _timescaleContainer = new PostgreSqlBuilder(
        "timescale/timescaledb:latest-pg18"
    ).Build();

    public async ValueTask InitializeAsync()
    {
        await _timescaleContainer.StartAsync();

        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        await ctx.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _timescaleContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.ConfigureDbContext<LorisDbContext>(options =>
                options.UseNpgsql(_timescaleContainer.GetConnectionString())
            );

            var hostedServiceDescriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // NOTE: THIS IS FOR DEBUGGING ONLY
            services.AddHostedService<DebuggingJob>();
        });
    }
}
