using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataNiuKnife
{
   
        public static class HostExtensions
        {
            public static IHostBuilder UseHostedService<T>(this IHostBuilder hostBuilder)
                where T : class, IHostedService, IDisposable
            {
                return hostBuilder.ConfigureServices(services =>
                    services.AddHostedService<T>());
            }
        }
}
