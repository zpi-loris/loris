using Loris.Data;
using Loris.Types;
using Microsoft.EntityFrameworkCore;

namespace Loris.Extensions;

public static class LorisDbContextExtensions
{
    extension(LorisDbContext ctx)
    {
        public async Task<bool> TryBumpDeviceAsync(
            DevEui devEui,
            DateTime dateTime,
            CancellationToken stoppingToken
        )
        {
            int updatedRows = await ctx
                .Devices.Where(d => d.DevEui == devEui)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(d => d.LastSeen, dateTime),
                    stoppingToken
                );

            if (updatedRows > 0)
            {
                return true;
            }
            return false;
        }
    }
}
