using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogMan
{    /*
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
    /// 表示输出日志的级别
    /// </summary>
    public enum LogLevel : int
    {
        /// <summary>
        /// 未配置
        /// </summary>
        Unknown,
        /// <summary>
        /// 什么也不输出
        /// </summary>
        None,
        /// <summary>
        /// 一般消息
        /// </summary>
        Info,
        /// <summary>
        /// 警告
        /// </summary>
        Warn,
        /// <summary>
        /// 一般异常
        /// </summary>
        Error,
        /// <summary>
        /// 致命异常
        /// </summary>
        Fatal
    }
}
