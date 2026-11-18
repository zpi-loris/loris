using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Models;
using Loris.Protocol;

namespace Loris.Extensions;

public static class ChirpstackDeviceServiceClientExtensions
{
    public const int REQUEST_DEADLINE_SEC = 5;

    extension(DeviceService.DeviceServiceClient client)
    {
        public AsyncUnaryCall<EnqueueDeviceQueueItemResponse> EnqueueDownlinkAsync(
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

            return client.EnqueueAsync(
                req,
                cancellationToken: ct,
                deadline: DateTime.UtcNow.AddSeconds(REQUEST_DEADLINE_SEC)
            );
        }
    }
}
