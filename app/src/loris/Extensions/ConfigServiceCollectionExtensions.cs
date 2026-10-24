using System.Threading.Channels;
using app.Messages;

namespace app.Extensions;

public static class ConfigServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddChannels()
        {
            services.AddSingleton(device => Channel.CreateUnbounded<IChirpstackMessage>());
            services.AddSingleton(eventChannel => Channel.CreateUnbounded<IEventMessage>());
            services.AddSingleton(dbWriteChannel => Channel.CreateUnbounded<IDbWriteMessage>());

            return services;
        }
    }
}
