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
    /// 表示用于json配置文件的MYSQL集群配置信息类
    /// <para>分库是扩展存储容量</para>
    /// <para>从库是数据镜像和读分流</para>
    /// </summary>
    public class MySqlClusterSettings
    {
        List<MySqlNode> m_Nodes;

        public MySqlClusterSettings()
        {
            m_Nodes = new List<MySqlNode>();
        }

        /// <summary>
        /// 配置中的节点清单
        /// </summary>
        public List<MySqlNode> Nodes { get { return m_Nodes; } set { m_Nodes = value; } }

        /// <summary>
        /// 取主库
        /// </summary>
        /// <param name="dbname">数据库名称</param>
        /// <returns></returns>
        public MySqlNode GetMaster(string dbname)
        {
            var temp = m_Nodes.Find(n => n.IsSlave == false & n.DevideFromNodeID == 0 & n.DataBasesName.ToLower().Equals(dbname.ToLower()));
            return temp;
        }

        /// <summary>
        /// 取从库
        /// </summary>
        /// <param name="dbname">数据库名称</param>
        /// <returns></returns>
        public MySqlNode GetSlave(string dbname)
        {
            var temp = m_Nodes.Find(n => n.IsSlave == true & n.DevideFromNodeID == 0 & n.DataBasesName.ToLower().Equals(dbname.ToLower()));
            return temp;
        }

        /// <summary>
        /// 取分库
        /// </summary>
        /// <param name="dbname">数据库名称</param>
        /// <param name="devideFromMasterID">主库节点ID</param>
        /// <returns></returns>
        public MySqlNode GetDevide(string dbname, int devideFromMasterID)
        {
            var temp = m_Nodes.Find(n => n.DevideFromNodeID == devideFromMasterID & n.DataBasesName.ToLower().Equals(dbname.ToLower()));
            return temp;
        }
    }
}
