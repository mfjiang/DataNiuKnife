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
    /// 表示用于json配置文件的MYSQL集群配置节点信息类
    /// </summary>
    public class MySqlNode
    {
        public MySqlNode() { }

        /// <summary>
        /// 节点在配置中的ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 是否为从库节点
        /// </summary>
        public bool IsSlave { get; set; }

        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DataBasesName { get; set; }

        /// <summary>
        /// 连接串
        /// </summary>
        public string ConnStr { get; set; }

        /// <summary>
        /// 分库的主库ID（0表示非分库）
        /// </summary>
        public int DevideFromNodeID { get; set; }

        /// <summary>
        /// 分库的分表设置例：table 1:hash key,table 2:hash key,table n:hash key
        /// </summary>
        public string DevideDataSet { get; set; }

        /// <summary>
        /// 需要自动迁移的大表例：table_name=t1,key_name=key1,date_field=created,data_hold_days=30,archive_node_id=5,schedule_time=23:00:00:00;
        /// </summary>
        public string AutoMoveDataSet { get; set; }
    }
}
