using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataNiuKnife
{
    /*
Copyright (C)  2019 Jiang Ming Feng
Github: https://github.com/mfjiang
Contact: hamlet.jiang@live.com
License:  https://github.com/mfjiang/DataNiuKnife/blob/master/LICENSE

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at
http://www.apache.org/licenses/LICENSE-2.0
*/


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
