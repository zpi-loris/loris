using Loris.Models;
using Loris.Results;

namespace Loris.Services;

public interface IDeviceService
{
    public Task<Result<Device>> AcceptAsync(DevEui devEui, CancellationToken ct = default);
    public Task<Result<Device>> AcceptAsync(string name, CancellationToken ct = default);
    public Task<Result<Device>> DisconnectAsync(
        DevEui devEui,
        bool force,
        CancellationToken ct = default
    );
    public Task<Result<Device>> DisconnectAsync(
        string name,
        bool force,
        CancellationToken ct = default
    );
    public Task<Result<Device>> ToggleIgnoreAsync(
        DevEui devEui,
        bool ignore,
        CancellationToken ct = default
    );
    public Task<Result<Device>> ToggleIgnoreAsync(
        string name,
        bool ignore,
        CancellationToken ct = default
    );
    public Task<Result<IEnumerable<Device>>> ListDevicesAsync(
        List<DeviceStatus> statusFilterList,
        CancellationToken ct = default
    );
    public Task<Result<Device>> GetAsync(DevEui devEui, CancellationToken ct = default);
    public Task<Result<Device>> GetAsync(string name, CancellationToken ct = default);
    public Task<Result<Device>> GetAsync(Guid deviceId, CancellationToken ct = default);
    public Task<Result<Device>> RenameAsync(
        DevEui devEui,
        string newName,
        CancellationToken ct = default
    );
    public Task<Result<Device>> RenameAsync(
        string name,
        string newName,
        CancellationToken ct = default
    );
    public Task<Result<Device>> RenameAsync(
        Guid deviceId,
        string newName,
        CancellationToken ct = default
    );
}
