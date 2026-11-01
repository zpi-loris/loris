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
            var host = configuration["Host"];

            if (host == null)
            {
                throw new ArgumentNullException(
                    paramName: host,
                    message: "Chirpstack integration host environment variable is not set"
                );
            }

            var settings = new ClientSettings { Host = host };

            if (int.TryParse(configuration["Port"], out var port))
            {
                settings = settings with { Port = port };
            }

            if (Guid.TryParse(configuration["ApplicationId"], out var applicationId))
            {
                settings = settings with { ApplicationId = applicationId };
            }

            if (int.TryParse(configuration["ReconnectInterval"], out var reconnectInterval))
            {
                settings = settings with { ReconnectInterval = reconnectInterval };
            }

            services.AddSingleton(settings);

            return services;
        }
    }
}
