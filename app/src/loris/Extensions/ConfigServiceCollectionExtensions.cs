using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Loris.Messages.Database;
using Loris.Messages.Device;
using Loris.Messages.Event;
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
            var conf = configuration.GetSection("Chirpstack").GetSection("Integration");

            var host = conf["Host"];

            if (host == null)
            {
                throw new ArgumentNullException(
                    paramName: host,
                    message: "Chirpstack integration host environment variable is not set"
                );
            }

            if (!int.TryParse(conf["Port"], out int port))
            {
                port = 1833;
            }

            if (!int.TryParse(conf["ReconnectInterval"], out var reconnectInterval))
            {
                reconnectInterval = 5;
            }

            if (!int.TryParse(conf["ConnectTimeout"], out var connectTimeout))
            {
                connectTimeout = 5;
            }

            // All important settings
            var settings = new ChirpstackIntegrationSettings
            {
                Host = host,
                Port = port,
                ReconnectInterval = reconnectInterval,
                ConnectTimeout = connectTimeout,
            };

            // filtering by application id
            if (Guid.TryParse(conf["ApplicationId"], out var applicationId))
            {
                settings = settings with { ApplicationId = applicationId };
            }

            services.AddSingleton(settings);

            services.AddSingleton<IChirpstackIntegrationHandler, THandler>();

            services.AddHostedService<ChirpstackIntegration>();

            return services;
        }
    }
}
