using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogMan
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
    /// 表示自定义日志输出的属性类
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, Inherited = false)]
    public class LogAttribute : Attribute
    {
        /// <summary>
        /// 获取或设置日志级别
        /// </summary>
        public LogLevel LogLevel { get; set; }

        /// <summary>
        /// 获取或设置日志名
        /// </summary>
        public string LogName { get; set; }

        /// <summary>
        /// 获取或设置日志文件后缀 如 .log
        /// </summary>
        public string FileSuffix { get; set; }

        /// <summary>
        /// 获取或设置自动清理日志的天数
        /// </summary>
        public int AutoCleanDays { get; set; }
    }
}
