using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Models;
using Loris.Protocol;
using Loris.Results;
using Loris.Results.Errors;

namespace Loris.Extensions;

public static class ChirpstackDeviceServiceClientExtensions
{
    public const int REQUEST_DEADLINE_SEC = 5;

    extension(DeviceService.DeviceServiceClient client)
    {
        public async Task<Result> EnqueueDownlinkAsync(
            Downlink downlink,
            DevEui devEui,
            CancellationToken ct = default
        )
        {
            var req = new EnqueueDeviceQueueItemRequest()
            {
                QueueItem = new DeviceQueueItem()
                {
                    Confirmed = false,
                    Data = downlink.ToByteString(),
                    DevEui = devEui.ToString(),
                    FPort = 1,
                },
            };

            try
            {
                await client.EnqueueAsync(
                    req,
                    cancellationToken: ct,
                    deadline: DateTime.UtcNow.AddSeconds(REQUEST_DEADLINE_SEC)
                );
            }
            catch (RpcException ex)
            {
                return ex.StatusCode switch
                {
                    StatusCode.DeadlineExceeded => Result.Failure(
                        DownlinkErrors.DeadlineExceeded()
                    ),
                    //
                    // TODO: apparently chirpstack is sending unauthenticated, even
                    // when the token is valid, but it can't find any device with the given DevEUI.
                    // This is a bug in chirpstack and i don't know how to handle it yet.
                    StatusCode.Unauthenticated => Result.Failure(
                        DownlinkErrors.Unauthenticated(devEui)
                    ),

                    // TODO: Handle other exceptions if needed
                    _ => Result.Failure(DownlinkErrors.Failed(ex.Message)),
                };
            }

            return Result.Success();
        }
    }
}
