using CmdScale.EntityFrameworkCore.TimescaleDB.Query;
using Loris.Controllers.Responses;
using Loris.Data;
using Loris.Protocol;
using Loris.Results;
using Loris.Results.Errors;
using Microsoft.EntityFrameworkCore;

namespace loris.Services;

public class SensorService(LorisDbContext ctx) : ISensorService
{
    private readonly LorisDbContext _ctx = ctx;

    public async Task<Result<IEnumerable<SensorReadingResponse>>> GetSensorReadings(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    )
    {
        var sensor = await _ctx
            .Sensors.AsNoTracking()
            .Select(s => new { s.SensorId, s.ValueType })
            .FirstOrDefaultAsync(s => s.SensorId == sensorId, ct);

        if (sensor is null)
        {
            return Result<IEnumerable<SensorReadingResponse>>.Failure(
                SensorErrors.NotFound(sensorId)
            );
        }

        var readings = _ctx
            .SensorReadings.AsNoTracking()
            .Where(r => r.SensorId == sensor.SensorId && r.Time >= from && r.Time < to);

        var response = sensor.ValueType switch
        {
            SensorValueType.Float => readings.Select(r => new SensorReadingResponse
            {
                Time = r.Time,
                Value = r.FloatValue!,
            }),
            SensorValueType.Int => readings.Select(r => new SensorReadingResponse
            {
                Time = r.Time,
                Value = r.IntValue!,
            }),
            SensorValueType.Bool => readings.Select(r => new SensorReadingResponse
            {
                Time = r.Time,
                Value = r.BoolValue!,
            }),
            SensorValueType.Text => readings.Select(r => new SensorReadingResponse
            {
                Time = r.Time,
                Value = r.StringValue!,
            }),
            _ => throw new NotImplementedException($"ValueType {sensor.ValueType} not implemented"),
        };

        return await response.OrderBy(r => r.Time).ToListAsync(ct);
    }

    public async Task<Result<IEnumerable<SensorReadingAverageResponse>>> GetSensorReadingsAverage(
        Guid sensorId,
        DateTime from,
        DateTime to,
        TimeSpan timeInterval,
        CancellationToken ct
    )
    {
        var sensor = await _ctx
            .Sensors.AsNoTracking()
            .Select(s => new { s.SensorId, s.ValueType })
            .FirstOrDefaultAsync(s => s.SensorId == sensorId, ct);

        if (sensor is null)
        {
            return Result<IEnumerable<SensorReadingAverageResponse>>.Failure(
                SensorErrors.NotFound(sensorId)
            );
        }

        if (sensor.ValueType != SensorValueType.Float || sensor.ValueType != SensorValueType.Int)
        {
            return Result<IEnumerable<SensorReadingAverageResponse>>.Failure(
                SensorErrors.NotNumeric(sensorId, sensor.ValueType.ToString())
            );
        }

        var readings = _ctx
            .SensorReadings.AsNoTracking()
            .Where(r => r.SensorId == sensor.SensorId && r.Time >= from && r.Time < to)
            .GroupBy(r => new
            {
                Bucket = EF.Functions.TimeBucket(timeInterval, r.Time),
                r.SensorId,
            });

        var response = sensor.ValueType switch
        {
            SensorValueType.Float => readings.Select(g => new SensorReadingAverageResponse
            {
                Time = g.Key.Bucket,
                Average = g.Average(r => r.FloatValue)!,
                Min = g.Min(r => r.FloatValue)!,
                Max = g.Max(r => r.FloatValue)!,
                Count = g.Count(),
            }),
            SensorValueType.Int => readings.Select(g => new SensorReadingAverageResponse
            {
                Time = g.Key.Bucket,
                Average = g.Average(r => r.IntValue)!,
                Min = g.Min(r => r.IntValue)!,
                Max = g.Max(r => r.IntValue)!,
                Count = g.Count(),
            }),
            _ => throw new NotImplementedException($"ValueType {sensor.ValueType} not implemented"),
        };

        return await response.OrderBy(r => r.Time).ToListAsync(ct);
    }
}
