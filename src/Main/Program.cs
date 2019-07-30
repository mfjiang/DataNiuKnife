using DotNetCoreConfiguration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DataNiuKnife
{
    public class Program
    {
        private static bool canclePress = false;
        private static DateTime m_ProgramStart;
        private static DateTime m_CmdStart;
        private static string title;
        private static string version;
        private static string cmd;
        private static string[] cmd_args;
        private static IScheduler scheduler;
        private static IDataNiuKnifeService<AppSettings> service;

        //Quartz.NET Quick Start Guide
        //https://www.quartz-scheduler.net/documentation/quartz-3.x/quick-start.html
        //https://blog.csdn.net/aofengdaxia/article/details/79789028
        //https://www.cnblogs.com/yaopengfei/p/9216229.html


        #region 命令交互模式入口
        public static void Main(string[] args)
        //static async Task Main(string[] args)
        {
            //取版本号
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            version = fvi.FileVersion;
            m_ProgramStart = DateTime.Now;
            title = "Data Niu-Knife（数据牛刀，大数据表自动分割服务）";

            cmd = String.Empty;
            cmd_args = new string[] { };

            Console.Title = title;
            PrintScreen();

            //前台启动服务
            //service = new NiuKnifeService(ConfigurationManager.GetAppConfig());
            //service.RunAsync();

            //创建后台服务
            //https://docs.microsoft.com/zh-cn/aspnet/core/fundamentals/host/generic-host?view=aspnetcore-2.2
            var builder = new HostBuilder()
            //host config
            .ConfigureHostConfiguration(config =>
            {
                config.SetBasePath(Directory.GetCurrentDirectory());
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddEnvironmentVariables(prefix: "PREFIX_");
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                config.AddEnvironmentVariables();

                //command line
                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            //app config
            .ConfigureAppConfiguration((hostContext, config) =>
            {
                var env = hostContext.HostingEnvironment;
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

                config.AddEnvironmentVariables();

                if (args != null)
                {
                    config.AddCommandLine(args);
                }
            })
            //service
            .ConfigureServices((hostContext, services) =>
            {
                services.AddOptions();
                services.AddHostedService<NiuKnifeService>();//必须实现自IHosted接口
                services.Configure<AppSettings>(o => ConfigurationManager.GetAppConfig());
            });

            //console 
            builder.RunConsoleAsync();

            //始终保持一个等待的线程，以免容器内自动退出
            SpinWait.SpinUntil(() => false);            
            //Thread thread = new Thread(MySpinWait);
            //thread.Start();

            while (!canclePress)
            {
                try
                {
                    if (String.IsNullOrEmpty(cmd))
                    {
                        cmd = Console.ReadLine().ToLower();
                    }
                    string gname = "";
                    cmd_args = cmd.Split(new string[] { " ", ":" }, StringSplitOptions.RemoveEmptyEntries);
                    if (cmd_args.Length == 3)
                    {
                        cmd = cmd_args[0];
                    }

                    switch (cmd)
                    {
                        case "quit":
                            canclePress = true;
                            Console.WriteLine(String.Format("{0} >{1}", DateTime.Now, "正在退出"));
                            break;
                        case "exit":
                            canclePress = true;
                            Console.WriteLine(String.Format("{0} >{1}", DateTime.Now, "正在退出"));
                            break;
                        case "test":
                            //测试
                            Console.Clear();
                            m_CmdStart = DateTime.Now;
                            PrintScreen();
                            Console.WriteLine(String.Format("{0} >正在执行{1}……", DateTime.Now, cmd));
                            Test();
                            cmd = Console.ReadLine().ToLower();
                            break;
                        case "status":
                            Console.Clear();
                            m_CmdStart = DateTime.Now;
                            PrintScreen();
                            Console.WriteLine(String.Format("{0} >正在执行{1}……", DateTime.Now, cmd));
                            Status();
                            cmd = Console.ReadLine().ToLower();
                            break;
                        default:
                            cmd = Console.ReadLine().ToLower();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex);
                    cmd = Console.ReadLine().ToLower();
                }
            }

        }
        #endregion

        #region 事件处理

        //private static void Updrg_OnReportRowAdded(string msg)
        //{
        //    PrintScreen();
        //    int pos = 14;
        //    Console.SetCursorPosition(0, pos);
        //    Console.WriteLine(String.Format("{0} > {1}", DateTime.Now, msg));
        //}

        /// <summary>
        /// Ctr+C 关闭 .netframework有效
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            //Stop
            canclePress = true;
            if (scheduler != null && !scheduler.IsShutdown)
            {
                //强制退出
                scheduler.Shutdown(false);
            }
            Console.WriteLine("正在关闭");
            Console.ReadKey();
        }
        #endregion

        
        /// <summary>
        /// docker中无效
        /// </summary>
        private static void MySpinWait()
        {            
            while (!canclePress)
            {
                Thread.Sleep(1000);                
            }
        }

        private static void Test()
        {
            var nodes = ConfigurationManager.GetMySqlClusterSettings();
            for (int i = 0; i < nodes.Nodes.Count; i++)
            {
                string configs = nodes.Nodes[i].AutoMoveDataSet;
                List<AutoMoveDataConfig> ls = AutoMoveDataConfig.Parse(configs);
                Console.WriteLine(String.Format("节点:{1},ID:{2}，共{0}个配置表", ls.Count, nodes.Nodes[i].DataBasesName, nodes.Nodes[i].ID));
                for (int r = 0; r < ls.Count; r++)
                {
                    Console.WriteLine(String.Format("表{0}:{1},数据保鲜期:{2}天,时间标识列:{3},任务计划时间:{4},归档节点ID:{5}", r + 1, ls[r].TableName, ls[r].DataHoldDays, ls[r].DateField, ls[r].ScheduleTime, ls[r].ArchiveNodeID));
                }
            }
        }

        private static void Status()
        {
            Console.WriteLine("work path:");
            Console.WriteLine(AppDomain.CurrentDomain.BaseDirectory);
            Console.WriteLine(AppDomain.CurrentDomain.DynamicDirectory);
            Console.WriteLine("log path:");
            string logpath = ConfigurationManager.GetAppConfig("LogManPath");
            Console.WriteLine(String.Format("{0},existed? {1}", logpath, Directory.Exists(logpath)));
            Console.WriteLine("");
            var nodes = ConfigurationManager.GetMySqlClusterSettings();
            for (int i = 0; i < nodes.Nodes.Count; i++)
            {
                string configs = nodes.Nodes[i].AutoMoveDataSet;
                List<AutoMoveDataConfig> ls = AutoMoveDataConfig.Parse(configs);
                Console.WriteLine(String.Format("节点:{1},ID:{2}，共{0}个配置表", ls.Count, nodes.Nodes[i].DataBasesName, nodes.Nodes[i].ID));
                for (int r = 0; r < ls.Count; r++)
                {
                    Console.WriteLine(String.Format("表{0}:{1},数据保鲜期:{2}天,时间标识列:{3},任务计划时间:{4},归档节点ID:{5}", r + 1, ls[r].TableName, ls[r].DataHoldDays, ls[r].DateField, ls[r].ScheduleTime, ls[r].ArchiveNodeID));
                }
            }

            Console.WriteLine("");
            Console.WriteLine("任务计划如下：");
            if (service == null)
            {
                var s = AppDomain.CurrentDomain.GetData("service");
                if (s != null)
                {
                    service = (IDataNiuKnifeService<AppSettings>)s;
                }
            }

            if (service != null)
            {
                for (int i = 0; i < service.JobList.Count; i++)
                {
                    Console.WriteLine(service.JobList[i].Description);
                }
            }
            else
            {
                Console.WriteLine("there is no job service instances.");
            }
        }

        /// <summary>
        /// 打印屏幕信息
        /// </summary>
        private static void PrintScreen()
        {
            Console.Clear();
            Console.WriteLine(title);
            Console.WriteLine("Ver.Beta " + version);
            Console.WriteLine(String.Format("{0}", m_ProgramStart));
            Console.WriteLine("==================================================================");
            Console.WriteLine("命令清单:");
            Console.WriteLine("quit/exit 退出");
            Console.WriteLine("status 状态");
            Console.WriteLine("test 测试");
            Console.WriteLine("==================================================================");

            if (!String.IsNullOrEmpty(cmd))
            {
                if (cmd_args.Length != 3)
                {
                    Console.WriteLine(String.Format("执行:{0} {1} Ctr+C 退出", cmd, DateTime.Now - m_CmdStart));
                    Console.Title = String.Format("{0} {1}", title, cmd);
                }
                else
                {
                    Console.WriteLine(String.Format("执行:{0} {1}:{2} {3} Ctr+C 退出", cmd, cmd_args[1], cmd_args[2], DateTime.Now - m_CmdStart));
                    Console.Title = String.Format("{0} {1} {2}:{3}", title, cmd, cmd_args[1], cmd_args[2]);
                }
            }
            else
            {
                Console.WriteLine("");
            }
        }
    }
}
