using Loris.Dtos.Device;
using Loris.Models;
using Loris.Results;

namespace Loris.Services;

public interface IDeviceService
{
    public Task<Result<DeviceResponse>> AcceptAsync(Guid deviceId, CancellationToken ct = default);
    public Task<Result<DeviceResponse>> AcceptAsync(DevEui devEui, CancellationToken ct = default);
    public Task<Result<DeviceResponse>> AcceptAsync(string name, CancellationToken ct = default);
    public Task<Result<DeviceResponse>> DisconnectAsync(
        Guid deviceId,
        bool force,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> DisconnectAsync(
        DevEui devEui,
        bool force,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> DisconnectAsync(
        string name,
        bool force,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> ToggleIgnoreAsync(
        Guid id,
        bool ignore,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> ToggleIgnoreAsync(
        DevEui devEui,
        bool ignore,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> ToggleIgnoreAsync(
        string name,
        bool ignore,
        CancellationToken ct = default
    );
    public Task<Result<IEnumerable<DeviceDetailsResponse>>> ListDevicesAsync(
        List<DeviceStatus> statusFilterList,
        CancellationToken ct = default
    );
    public Task<Result<DeviceDetailsResponse>> GetAsync(
        DevEui devEui,
        CancellationToken ct = default
    );
    public Task<Result<DeviceDetailsResponse>> GetAsync(
        string name,
        CancellationToken ct = default
    );
    public Task<Result<DeviceDetailsResponse>> GetAsync(
        Guid deviceId,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> RenameAsync(
        DevEui devEui,
        string newName,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> RenameAsync(
        string name,
        string newName,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> RenameAsync(
        Guid deviceId,
        string newName,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> ReplaceAsync(
        Guid deviceId,
        DeviceReplaceRequest request,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> ReplaceAsync(
        string name,
        DeviceReplaceRequest request,
        CancellationToken ct = default
    );
    public Task<Result<DeviceResponse>> ReplaceAsync(
        DevEui devEui,
        DeviceReplaceRequest request,
        CancellationToken ct = default
    );
}
