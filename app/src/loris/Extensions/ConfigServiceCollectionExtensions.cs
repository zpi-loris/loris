using System.Threading.Channels;
using app.Messages;

namespace app.Extensions;

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

            var chirpstackChannel = Channel.CreateBounded<IChirpstackMessage>(boundedOptions);
            var dbWriteChannel = Channel.CreateBounded<IDbWriteMessage>(boundedOptions);
            var eventChannel = Channel.CreateBounded<IEventMessage>(boundedOptions);

            services.AddSingleton(chirpstackChannel.Writer);
            services.AddSingleton(chirpstackChannel.Reader);

            services.AddSingleton(dbWriteChannel.Writer);
            services.AddSingleton(dbWriteChannel.Reader);

            services.AddSingleton(eventChannel.Writer);
            services.AddSingleton(eventChannel.Reader);

            return services;
        }
    }
}
