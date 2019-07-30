using DotNetCoreConfiguration;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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

    /// <summary>
    /// 表示NiuKnifeService的接口
    /// </summary>
    /// <typeparam name="TAppSettings"></typeparam>
    public interface IDataNiuKnifeService<TAppSettings>
    {
        /// <summary>
        /// 用于当前服务的AppSettings实例
        /// </summary>
        TAppSettings AppSettings { get; }

        /// <summary>
        /// 用于当前服务的MySqlClusterSettings实例
        /// </summary>
        MySqlClusterSettings MySqlClusterSettings { get; }

        /// <summary>
        /// 设置停止标识
        /// </summary>
        bool IsStop { get; set; }

        /// <summary>
        /// 定时任务清单 
        /// </summary>
        List<IJobDetail> JobList { get; }

        /// <summary>
        /// 前台模式异步启动
        /// </summary>
        /// <returns></returns>
        Task RunAsync();
    }
}
