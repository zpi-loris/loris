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
            var conf = configuration.GetSection("Chirpstack").GetSection("Integration");

            var host = conf.TryGetPropertyValue<string?>("Host");
            var port = conf.TryGetPropertyValue("Port", 1883);
            var applicationId = conf.TryGetPropertyValue<Guid?>("ApplicationId");
            var reconnectInterval = conf.TryGetPropertyValue("ReconnectInterval", 5);
            var connectTimeout = conf.TryGetPropertyValue("ConnectTimeout", 5);

            if (host == null)
            {
                throw new ArgumentNullException(
                    paramName: host,
                    message: "Chirpstack integration host environment variable is not set"
                );
            }

            var settings = new ChirpstackIntegrationSettings
            {
                Host = host,
                Port = port,
                ApplicationId = applicationId,
                ReconnectInterval = reconnectInterval,
                ConnectTimeout = connectTimeout,
            };

            services.AddSingleton(settings);

            services.AddSingleton<IChirpstackIntegrationHandler, THandler>();

            services.AddHostedService<ChirpstackIntegration>();

            return services;
        }
    }
}
