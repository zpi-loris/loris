using MQTTnet;

namespace Loris.MQTT.Extensions;

public static class IMqttClientExtensions
{
    extension(IMqttClient client)
    {
        public Task WaitForDisconnectedAsync(CancellationToken cancellationToken)
        {
            var taskCompletionSource = new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously
            );

            Func<MqttClientDisconnectedEventArgs, Task>? handler = null;
            CancellationTokenRegistration registration = default;

            handler = (details) =>
            {
                client.DisconnectedAsync -= handler;
                taskCompletionSource.TrySetResult();
                return Task.CompletedTask;
            };

            client.DisconnectedAsync += handler;

            registration = cancellationToken.Register(() =>
            {
                client.DisconnectedAsync -= handler;
                taskCompletionSource.TrySetCanceled();
            });

            return taskCompletionSource.Task;
        }
    }
}
