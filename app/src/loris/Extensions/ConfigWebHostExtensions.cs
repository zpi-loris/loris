using System.Net.Sockets;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace loris.Extensions;

public static class ConfigWebHostExtensions
{
    extension(IWebHostBuilder builder)
    {
        public IWebHostBuilder ConfigureLorisUnixSocket(string socketPath)
        {
            builder.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenUnixSocket(
                    socketPath,
                    listenOptions =>
                    {
                        listenOptions.Protocols = HttpProtocols.Http1;
                        listenOptions.Use(
                            async (context, next) =>
                            {
                                if (context.LocalEndPoint is UnixDomainSocketEndPoint unixEndPoint)
                                {
                                    context.Items["UnixSocketPath"] = unixEndPoint.ToString();
                                }
                                await next(context);
                            }
                        );
                    }
                );
            });

            return builder;
        }

        public IWebHostBuilder ConfigureLorisLocalhost(int port)
        {
            builder.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenLocalhost(port);
            });

            return builder;
        }
    }
}
