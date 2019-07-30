using DotNetCoreConfiguration;
using Quartz;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataNiuKnife
{
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
