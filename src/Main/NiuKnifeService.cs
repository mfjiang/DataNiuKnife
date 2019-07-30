using DotNetCoreConfiguration;
using LogMan;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
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

    //从配置中解析要分割数据的源库、表名、数据保鲜期、日期字段名、主键字段名、归档库、分割作业的时间点    
    //使用Quartz.net实现定时任务
    //分割操作包括数据定期复制，和过期数据定期清理

    /// <summary>
    /// 主服务类
    /// </summary>
    [LogAttribute(FileSuffix = ".log", LogLevel = LogLevel.Info, LogName = "NiuKnifeService", AutoCleanDays = 7)]
    public class NiuKnifeService : BackgroundService, IDataNiuKnifeService<AppSettings>
    {
        #region 私有字段
        private IScheduler m_Scheduler;
        private readonly AppSettings m_Settings;
        private readonly MySqlClusterSettings m_MySqlClusterSettings;
        private Dictionary<MySqlNode, List<AutoMoveDataConfig>> m_ConfiguredDataNode;
        private List<IJobDetail> m_JobList;
        private bool m_Stop = true;
        #endregion

        #region 属性
        /// <summary>
        /// 用于当前服务的AppSettings实例
        /// </summary>
        public AppSettings AppSettings { get { return m_Settings; } }

        /// <summary>
        /// 用于当前服务的MySqlClusterSettings实例
        /// </summary>
        public MySqlClusterSettings MySqlClusterSettings { get { return m_MySqlClusterSettings; } }

        /// <summary>
        /// 定时任务清单 
        /// </summary>
        public List<IJobDetail> JobList { get { return m_JobList; } }

        /// <summary>
        /// 设置停止标识
        /// </summary>
        public bool IsStop { get { return m_Stop; } set { m_Stop = value; } }

        #endregion

        #region 构造器

        /// <summary>
        /// 依赖注入的构造器
        /// </summary>
        /// <param name="options">AppSettings配置实例</param>
        public NiuKnifeService(IOptions<AppSettings> options)
        {
            m_Settings = options.Value;
            m_Stop = false;
            m_MySqlClusterSettings = ConfigurationManager.GetMySqlClusterSettings();
            m_ConfiguredDataNode = new Dictionary<MySqlNode, List<AutoMoveDataConfig>>();
            m_JobList = new List<IJobDetail>();
            //背景服务host会自动调用ExecuteAsync入口
        }

        /// <summary>
        /// 非依赖注入构造
        /// </summary>
        /// <param name="appSettings"></param>
        public NiuKnifeService(AppSettings appSettings)
        {
            m_Settings = appSettings;
            m_Stop = false;
            m_MySqlClusterSettings = ConfigurationManager.GetMySqlClusterSettings();
            m_ConfiguredDataNode = new Dictionary<MySqlNode, List<AutoMoveDataConfig>>();
            m_JobList = new List<IJobDetail>();
            //始终保持一个等待的线程，以免容器内自动退出
            SpinWait.SpinUntil(() => false);
        }

        #endregion

        #region 前台模式启动
        /// <summary>
        /// 前台模式启动
        /// </summary>
        /// <returns></returns>
        public Task RunAsync()
        {
           return Task.Run(()=> StartServices());
        }

        #endregion

        #region 后台服务模式入口

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            m_Stop = stoppingToken.IsCancellationRequested;
            return Task.Run(() => StartServices(stoppingToken));
        }

        #endregion

        #region 任务处理

        /// <summary>
        /// 启动服务
        /// </summary>
        private async void StartServices(CancellationToken stoppingToken = default(CancellationToken))
        {
            //初始化过程……
            //按每个节点配置Job            
            //1.创建Schedule
            m_Scheduler = await StdSchedulerFactory.GetDefaultScheduler();
            Loger.Info(this.GetType(), "开始初始化服务……");

            try
            {
                //遍历所有节点，解析带有数据分割配置的节点创建job队列
                if (m_MySqlClusterSettings != null && m_MySqlClusterSettings.Nodes.Count > 0)
                {
                    foreach (var node in m_MySqlClusterSettings.Nodes)
                    {
                        if (!String.IsNullOrEmpty(node.AutoMoveDataSet))
                        {
                            List<AutoMoveDataConfig> ls = AutoMoveDataConfig.Parse(node.AutoMoveDataSet);
                            if (ls != null && ls.Count > 0)
                            {
                                //m_ConfiguredDataNode.Add(node, ls);
                                for (int k = 0; k < ls.Count; k++)
                                {
                                    //2.创建job (具体的job需要单独在一个文件中执行)
                                    var job = JobBuilder.Create<MysqlDataWorker>()
                                            .UsingJobData("conn_source_str", node.ConnStr)
                                            .UsingJobData("conn_dest_str", m_MySqlClusterSettings.Nodes.FindLast(o => o.ID.Equals(ls[k].ArchiveNodeID)).ConnStr)
                                            .UsingJobData("table_name", ls[k].TableName)
                                            .UsingJobData("key_name", ls[k].KeyName)
                                            .UsingJobData("data_hold_days", ls[k].DataHoldDays)
                                            .UsingJobData("date_field", ls[k].DateField)
                                            .WithIdentity("job_" + k, "g_" + node.DataBasesName)
                                            .WithDescription(String.Format("自动分割数据表:{0}，每天在{1}执行一次", ls[0].TableName, ls[k].ScheduleTime))
                                            .StoreDurably(true)
                                            .Build();

                                    m_JobList.Add(job);

                                    //3.创建触发器
                                    TimeOfDay timeOfDay = new TimeOfDay(ls[k].ScheduleTime.Hours, ls[k].ScheduleTime.Minutes, ls[k].ScheduleTime.Seconds);
                                    ITrigger trigger = TriggerBuilder.Create().WithDailyTimeIntervalSchedule(x => x.OnEveryDay().StartingDailyAt(timeOfDay).EndingDailyAfterCount(1)).Build();
                                    //ITrigger trigger_test = TriggerBuilder.Create().WithDailyTimeIntervalSchedule(x => x.OnEveryDay().WithInterval(1, IntervalUnit.Minute)).Build();
                                    await m_Scheduler.ScheduleJob(job, trigger);
                                    //await m_Scheduler.ScheduleJob(job, trigger_test);
                                }
                              
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Loger.Error(this.GetType(), "初始化服务失败，" + ex.Message, ex);
            }

            StringBuilder sb = new StringBuilder();
            if (m_JobList.Count > 0)
            {
                sb.AppendLine("开始初始化服务结束");
                for (int i = 0; i < m_JobList.Count; i++)
                {
                    sb.AppendLine(m_JobList[i].Description);
                }

                Loger.Info(this.GetType(), sb.ToString());
                await m_Scheduler.Start(stoppingToken);                
            }

            AppDomain.CurrentDomain.SetData("service", this);           
        }

        #endregion
    }
}
