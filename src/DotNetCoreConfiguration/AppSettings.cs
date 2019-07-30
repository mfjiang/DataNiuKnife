using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCoreConfiguration
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

    /// <summary>
    /// 对应appsettings.json中的AppSettings节
    /// </summary>
    public class AppSettings
    {
        public AppSettings() { }

        /// <summary>
        /// 日志路径
        /// </summary>
        public string LogManPath { get; set; }
    }
}
