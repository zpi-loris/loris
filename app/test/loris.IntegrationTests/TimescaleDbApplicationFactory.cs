using System.Data.Common;
using Loris.Data;
using Loris.Jobs;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using Testcontainers.PostgreSql;

namespace loris.IntegrationTests;

public sealed class TimescaleDbApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string CollectionKey = "HostWithTimescale";

    private readonly PostgreSqlContainer _timescaleContainer = new PostgreSqlBuilder(
        "timescale/timescaledb:latest-pg18"
    ).Build();

    private DbConnection _dbConnection = null!;
    private Respawner _respawner = null!;

    public async ValueTask InitializeAsync()
    {
        await _timescaleContainer.StartAsync();

        // apply migrations
        using var scope = Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        await ctx.Database.MigrateAsync();

        // setup respawner
        _dbConnection = new NpgsqlConnection(_timescaleContainer.GetConnectionString());
        await _dbConnection.OpenAsync();
        await InitializeRespawnerAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _timescaleContainer.DisposeAsync();
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await _respawner.ResetAsync(_dbConnection);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);
        Environment.SetEnvironmentVariable(
            "ConnectionStrings:Timescale",
            _timescaleContainer.GetConnectionString()
        );
        MockChirpstackConfiguration();
        builder.ConfigureServices(services =>
        {
            // remove prestartup jobs, since we don't have migrations and rest initialized
            var hostedServiceDescriptors = services
                .Where(descriptor => descriptor.ServiceType == typeof(IHostedService))
                .ToList();

            foreach (var descriptor in hostedServiceDescriptors)
            {
                services.Remove(descriptor);
            }

            // replace state cache with fake implementation with Clear() method
            var stateCacheDescriptor = services.FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(IStateCache)
            );
            services.Remove(stateCacheDescriptor!);
            services.AddSingleton<IStateCache, FakeStateCache>();
        });
    }

    private void MockChirpstackConfiguration()
    {
        Environment.SetEnvironmentVariable("Chirpstack:Integration:Host", "0.0.0.0");
        Environment.SetEnvironmentVariable("Chirpstack:DeviceService:Host", "0.0.0.0");
    }

    private async Task InitializeRespawnerAsync()
    {
        _respawner = await Respawner.CreateAsync(
            _dbConnection,
            new RespawnerOptions { SchemasToInclude = ["public"], DbAdapter = DbAdapter.Postgres }
        );
    }
}

[CollectionDefinition(TimescaleDbApplicationFactory.CollectionKey)]
public class DatabaseCollection : ICollectionFixture<TimescaleDbApplicationFactory> { }
