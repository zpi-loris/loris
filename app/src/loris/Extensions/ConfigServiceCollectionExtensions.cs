using System.Diagnostics.CodeAnalysis;
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

        public IServiceCollection AddChirpstackIntegration<THandler>(
            ConfigurationManager configuration
        )
            where THandler : class, IChirpstackIntegrationHandler
        {
            List<string> hierarchy = ["Chirpstack", "Integration"];

            var host = configuration[string.Join(":", hierarchy.Append("Host"))];

            if (host == null)
            {
                throw new ArgumentNullException(
                    paramName: host,
                    message: "Chirpstack integration host environment variable is not set"
                );
            }

            var settings = new ChirpstackIntegrationSettings { Host = host };

            if (
                int.TryParse(
                    configuration[string.Join(":", hierarchy.Append("Port"))],
                    out var port
                )
            )
            {
                settings = settings with { Port = port };
            }

            if (
                Guid.TryParse(
                    configuration[string.Join(":", hierarchy.Append("ApplicationId"))],
                    out var applicationId
                )
            )
            {
                settings = settings with { ApplicationId = applicationId };
            }

            if (
                int.TryParse(
                    configuration[string.Join(":", hierarchy.Append("ReconnectInterval"))],
                    out var reconnectInterval
                )
            )
            {
                settings = settings with { ReconnectInterval = reconnectInterval };
            }

            services.AddSingleton(settings);

            services.AddSingleton<IChirpstackIntegrationHandler, THandler>();

            services.AddHostedService<ChirpstackIntegration>();

            return services;
        }
    }
}
