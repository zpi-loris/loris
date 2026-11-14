using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Extensions;

public static class ChirpstackDeviceServiceClientExtensions
{
    extension(DeviceService.DeviceServiceClient client)
    {
        public AsyncUnaryCall<EnqueueDeviceQueueItemResponse> EnqueueDownlinkAsync(
            Downlink downlink,
            DevEui devEui,
            Metadata headers,
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

            return client.EnqueueAsync(req, headers: headers, cancellationToken: ct);
        }
    }
}
