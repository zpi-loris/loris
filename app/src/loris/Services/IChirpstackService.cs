using Loris.Models;
using Loris.Protocol;
using Loris.Results;

namespace loris.Services;

public interface IChirpstackService
{
    /// <summary>
    /// Enqueues a downlink <b> Accept </b> to the device queue in Chirpstack.
    /// </summary>
    /// <param name="devEui">The DevEUI of the device.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    /// <remarks>
    /// Use this method when you have a device's DevEUI <b>and invocation is not deffered.</b>
    /// </remarks>
    public Task<Result> AcceptAsync(DevEui devEui, CancellationToken ct = default);

    /// <summary>
    /// Enqueues a downlink <b> Accept </b> to the device queue in Chirpstack.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    /// <remarks>
    /// Use this method when you have a device's unique identifier
    /// <b>or when then invocation can be deffered.</b><br/>
    /// It will resolve the DevEUI from the cache before unqueuing.
    /// </remarks>
    public Task<Result> AcceptAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Enqueues a downlink <b> Disconnect </b> to the device queue in Chirpstack.
    /// </summary>
    /// <param name="devEui">The DevEUI of the device.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    /// <remarks>
    /// Use this method when you have a device's DevEUI <b>and invocation is not deffered.</b>
    /// </remarks>
    public Task<Result> DisconnectAsync(DevEui devEui, CancellationToken ct = default);

    /// <summary>
    /// Enqueues a downlink <b> Disconnect </b> to the device queue in Chirpstack.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    /// <remarks>
    /// Use this method when you have a device's unique identifier
    /// <b>or when then invocation can be deffered.</b><br/>
    /// It will resolve the DevEUI from the cache before unqueuing.
    /// </remarks>
    public Task<Result> DisconnectAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Enqueues a downlink <b> Call </b> to the device queue in Chirpstack.
    /// </summary>
    /// <param name="devEui">The DevEUI of the device.</param>
    /// <param name="message">The Call message to be sent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    /// <remarks>
    /// Use this method when you have a device's DevEUI <b>and invocation is not deffered.</b>
    /// </remarks>
    public Task<Result> CallFunctionAsync(
        DevEui devEui,
        Call message,
        CancellationToken ct = default
    );

    /// <summary>
    /// Enqueues a downlink <b> Call </b> to the device queue in Chirpstack.
    /// </summary>
    /// <param name="deviceId">The unique identifier of the device.</param>
    /// <param name="message">The Call message to be sent.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Result indicating success or failure.</returns>
    /// <remarks>
    /// Use this method when you have a device's unique identifier
    /// <b>or when then invocation can be deffered.</b><br/>
    /// It will resolve the DevEUI from the cache before unqueuing.
    /// </remarks>
    public Task<Result> CallFunctionAsync(
        Guid deviceId,
        Call message,
        CancellationToken ct = default
    );
}
