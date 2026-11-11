using System.Threading.Channels;
using Chirpstack.Api;
using Grpc.Core;
using Loris.Messages;
using Loris.MQTT.Client;
//
// resolve ambiguity with Grpc.Channel with alias
using AsyncChannel = System.Threading.Channels.Channel;

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

            var deviceChannel = AsyncChannel.CreateBounded<IDeviceMessage>(boundedOptions);
            var dbWriteChannel = AsyncChannel.CreateBounded<IDbWriteMessage>(boundedOptions);
            var eventChannel = AsyncChannel.CreateBounded<IEventMessage>(boundedOptions);

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

        public IServiceCollection AddChirpstackDeviceService(ConfigurationManager configuration)
        {
            var conf = configuration.GetSection("Chirpstack").GetSection("DeviceService");

            var host = conf["Host"];
            int port = int.TryParse(conf["Port"], out port) ? port : 8080;
            var token = conf["ApiToken"];

            ArgumentException.ThrowIfNullOrWhiteSpace(host, "Host");
            ArgumentException.ThrowIfNullOrWhiteSpace(host, "ApiToken");

            // register grpc client factory
            services
                .AddGrpcClient<DeviceService.DeviceServiceClient>(options =>
                {
                    options.Address = new Uri($"http://{host}:{port}");
                })
                .ConfigureChannel(options =>
                {
                    options.Credentials = ChannelCredentials.Insecure;
                });

            // TODO: add some initial service checking the validity of the token

            // register chirpstack api token for requests
            services.AddKeyedSingleton(
                new Metadata { { "Authorization", "Bearer" + token } },
                "chirpstack-api-token"
            );

            return services;
        }
    }
}
