using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace DataNiuKnife
{
    /// <summary>
    /// 表示大表自动数据迁移配置
    /// </summary>
    public class AutoMoveDataConfig
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 主键名
        /// </summary>
        public string KeyName { get; set; }
        /// <summary>
        /// 日期字段
        /// </summary>
        public string DateField { get; set; }
        /// <summary>
        /// 数据保鲜期（天数）
        /// </summary>
        public int DataHoldDays { get; set; }
        /// <summary>
        /// 数据归档节点ID
        /// </summary>
        public int ArchiveNodeID { get; set; }
        /// <summary>
        /// 计划时间（24小时之内的值）
        /// </summary>
        public TimeSpan ScheduleTime { get; set; }

        /// <summary>
        /// 从字符串中解析配置
        /// </summary>
        /// <param name="config">例：table_name=t1,key_name=key1,date_field=created,data_hold_days=30,archive_node_id=5,schedule_time=23:00:00:00;table_name=t2,key_name=key2,date_field=created,data_hold_days=30,archive_node_id=5,schedule_time=23:30:00:00;</param>
        /// <returns></returns>
        public static List<AutoMoveDataConfig> Parse(string config)
        {
            List<AutoMoveDataConfig> list = new List<AutoMoveDataConfig>();

            if (!String.IsNullOrEmpty(config))
            {
                string[] cfigs_1 = config.Split(";", StringSplitOptions.RemoveEmptyEntries);//第1层循环 ;
                if (cfigs_1.Length > 0)
                {
                    for (int i = 0; i < cfigs_1.Length; i++)
                    {
                        AutoMoveDataConfig cfg = new AutoMoveDataConfig();
                        string[] cfigs_2 = cfigs_1[i].Split(",", StringSplitOptions.RemoveEmptyEntries);//第2层循环 ,
                        if (cfigs_2.Length > 0)
                        {
                            for (int m = 0; m < cfigs_2.Length; m++)
                            {
                                string[] cfigs_3 = cfigs_2[m].Split("=", StringSplitOptions.RemoveEmptyEntries);//第3层循环 =
                                if (cfigs_3.Length == 2)
                                {
                                    string name = cfigs_3[0];
                                    string value = cfigs_3[1];
                                    switch (name.ToLower())
                                    {
                                        case "table_name":
                                            cfg.TableName = value;
                                            break;
                                        case "key_name":
                                            cfg.KeyName = value;
                                            break;
                                        case "date_field":
                                            cfg.DateField = value;
                                            break;
                                        case "data_hold_days":
                                            int dys = 30;
                                            int.TryParse(value, out dys);
                                            cfg.DataHoldDays = dys;
                                            break;
                                        case "archive_node_id":
                                            int nodeid = 1;
                                            int.TryParse(value, out nodeid);
                                            cfg.ArchiveNodeID = nodeid;
                                            break;
                                        case "schedule_time":
                                            string[] schedule_time_fields = value.Split(":", StringSplitOptions.RemoveEmptyEntries);
                                            int hours = 23;
                                            int mits = 59;
                                            int secs = 0;
                                            if (schedule_time_fields.Length > 0)
                                            {
                                                int.TryParse(schedule_time_fields[0], out hours);
                                                int.TryParse(schedule_time_fields[1], out mits);
                                                int.TryParse(schedule_time_fields[2], out secs);
                                            }
                                            cfg.ScheduleTime = TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(mits) + TimeSpan.FromSeconds(secs);
                                            break;
                                    }
                                }
                            }
                           
                        }
                        list.Add(cfg);
                    }

                }
            }

            return list;
        }
    }
}
