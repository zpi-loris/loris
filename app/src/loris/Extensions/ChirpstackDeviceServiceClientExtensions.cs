using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Protocol;
using Loris.Types;

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
