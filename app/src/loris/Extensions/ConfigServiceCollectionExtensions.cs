using System.Threading.Channels;
using Loris.Messages;
using Loris.MQTT.Client;
using Namotion.Reflection;

namespace Loris.Extensions;

public static class ConfigServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddChannels()
        {
            // NOTE: single options instance for tests only
            var boundedOptions = new BoundedChannelOptions(capacity: 5_000)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleWriter = false,
                SingleReader = true,
            };

            var deviceChannel = Channel.CreateBounded<IDeviceMessage>(boundedOptions);
            var dbWriteChannel = Channel.CreateBounded<IDbWriteMessage>(boundedOptions);
            var eventChannel = Channel.CreateBounded<IEventMessage>(boundedOptions);

            services.AddSingleton(deviceChannel.Writer);
            services.AddSingleton(deviceChannel.Reader);

            services.AddSingleton(dbWriteChannel.Writer);
            services.AddSingleton(dbWriteChannel.Reader);

            services.AddSingleton(eventChannel.Writer);
            services.AddSingleton(eventChannel.Reader);

            return services;
        }

        public IServiceCollection AddChirpstackIntegrationSettings(
            ConfigurationManager configuration
        )
        {
            var host = configuration.TryGetPropertyValue<string>("Host");
            var port = configuration.TryGetPropertyValue<int>("Port", 1883);
            var applicationId = configuration.TryGetPropertyValue<Guid?>("ApplicationId");

            services.AddSingleton(
                new ClientSettings
                {
                    Host = host,
                    Port = port,
                    ApplicationId = applicationId,
                }
            );

            return services;
        }
    }
}
