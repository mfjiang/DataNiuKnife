using DotNetCoreConfiguration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Data.OracleClient;
using MySql.Data;
using MySql.Data.MySqlClient;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data;
using LogMan;

namespace DataNiuKnife
{
    //Author    江名峰
    //Date      2019.07.22

    /// <summary>
    /// 表示定时处理的任务
    /// </summary>
    [LogAttribute(FileSuffix = ".log", LogLevel = LogLevel.Info, LogName = "MysqlDataWorker", AutoCleanDays = 7)]
    public class MysqlDataWorker : IJob
    {
        private IJobExecutionContext m_Context;
        private string m_ConnSourceStr;
        private string m_ConnDestStr;
        private string m_TableName;
        private string m_KeyName;
        private int m_DataHoldDays = 0;
        private string m_DateField;
        private long m_MaxCopy = 1000000;//一次复制100万记录     

        private string cmd_txt_create_split_tb = "CREATE TABLE IF NOT EXISTS `{0}` ";
        private string cmd_txt_select_dest_tb_schemas = "SELECT `TABLE_SCHEMA`,`TABLE_NAME`,`CREATE_TIME` FROM information_schema.`TABLES` as tables_schema WHERE tables_schema.`TABLE_NAME` LIKE '{0}_spt_%' ORDER BY tables_schema.`CREATE_TIME` DESC ";
        private string cmd_txt_select_dest_last_key = "SELECT `{0}` as keyname from `{1}` ORDER BY `{0}` DESC LIMIT 1";
        private string cmd_txt_show_create_tb = "SHOW CREATE TABLE `{0}`";
        private string cmd_txt_read_rows = "SELECT * FROM `{0}` WHERE `{1}` > {2} ORDER BY `{1}` ASC LIMIT {3}";
        private string cmd_txt_delete_rows = "DELETE FROM `{0}` WHERE `{1}`<= {2} AND DATEDIFF(now(),`{3}`) >={4}";

        /// <summary>
        /// 默认无参构造器
        /// </summary>
        public MysqlDataWorker()
        {

        }

        /// <summary>
        /// 测试用构造器
        /// </summary>
        /// <param name="connSource">源连接</param>
        /// <param name="connDest">归档库连接</param>
        /// <param name="tableName">表名</param>
        /// <param name="keyName">主键名</param>
        /// <param name="holdDays">数据保鲜天数</param>
        /// <param name="dateField">日期字段</param>
        public MysqlDataWorker(string connSource, string connDest, string tableName, string keyName, int holdDays, string dateField)
        {
            m_ConnSourceStr = connSource;
            m_ConnDestStr = connDest;
            m_TableName = tableName;
            m_KeyName = keyName;
            m_DateField = dateField;
            m_DataHoldDays = holdDays;
        }

        #region 异步方法，服务内调用
        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                m_Context = context;
                m_ConnSourceStr = (string)context.JobDetail.JobDataMap["conn_source_str"];
                m_ConnDestStr = (string)context.JobDetail.JobDataMap["conn_dest_str"];
                m_TableName = (string)context.JobDetail.JobDataMap["table_name"];
                m_KeyName = (string)context.JobDetail.JobDataMap["key_name"];
                m_DateField = (string)context.JobDetail.JobDataMap["date_field"];
                string temp = context.JobDetail.JobDataMap["data_hold_days"].ToString();
                int.TryParse(temp, out m_DataHoldDays);
            }
            catch (Exception ex)
            {
                Loger.Fatal(this.GetType(), ex.Message, ex);
            }

            await DayWork();
        }

        public async Task DayWork()
        {
            Task t = new Task(Work);
            t.Start();
            Loger.Info(this.GetType(), String.Format("MysqlDataWorker已在服务环境启动，源表名:" + m_TableName));
            await t;
        }
        #endregion

        /// <summary>
        /// 工作主体过程
        /// </summary>
        public void Work()
        {
            //使用默认分表编号规则，查询是否已经存在可用的分表
            //分表规则：总是按原表名按年月建表，因此随着时间流自然产生新一月的分表，例： MyTable_spt_20190701
            string temp_table = String.Format("{0}_spt_{1}", m_TableName, DateTime.Now.ToString("yyyyMM"));
            string[] tbs;
            try
            {
                if (!CheckSplitTableExists(m_TableName, temp_table, out tbs))
                {
                    //如果不存在可用的分表，自动创建
                    Loger.Info(this.GetType(), String.Format("开始自动创建分表，表名:" + temp_table));
                    CreateSplitTable(temp_table, m_TableName);
                    Loger.Info(this.GetType(), String.Format("成功自动创建分表，表名:" + temp_table));
                };

                //数据复制
                //从归档表的最后一个key+1为起点，从源表读取数据副本
                Loger.Info(this.GetType(), String.Format("开始自动复制数据，源表名:{0},分表名:{1}", m_TableName, temp_table));
                long total = 0;
                total = CopyData(temp_table, m_TableName);
                Loger.Info(this.GetType(), String.Format("自动复制了{2}笔数据，源表名:{0},分表名:{1}", m_TableName, temp_table, total));

                //数据清理
                //从归档表的最后一个key为终点，从源表清除过期数据
                if (tbs != null && tbs.Length > 0)
                {
                    long r = 0;
                    Loger.Info(this.GetType(), String.Format("开始自动清理数据，表名:" + m_TableName));
                    r = DeleteData(tbs[tbs.Length - 1], m_TableName);
                    Loger.Info(this.GetType(), String.Format("完成自动清理数据{0}笔，表名:{1}", r, m_TableName));
                }
            }
            catch (Exception ex)
            {
                Loger.Error(this.GetType(), String.Format("任务执行失败，源表名:{0}，分表名:{1}", m_TableName, temp_table), ex);
            }
        }

        /// <summary>
        /// 检查一个表是否存在指定的分表
        /// </summary>
        /// <param name="sourceTableName">源表名</param>
        /// <param name="splitTableName">新的分表名</param>
        /// <param name="tbs">返回找到的分表名数组</param>
        /// <returns></returns>
        public bool CheckSplitTableExists(string sourceTableName, string splitTableName, out string[] tbs)
        {
            bool found = false;
            tbs = new string[] { };
            try
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnDestStr))
                {
                    conn.Open();
                    var command = String.Format(cmd_txt_select_dest_tb_schemas, sourceTableName);
                    DataSet ds = MySqlHelper.ExecuteDataset(conn, command);
                    if (ds != null && ds.Tables.Count > 0)
                    {
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            string temp = "";
                            for (int k = 0; k < ds.Tables[0].Rows.Count; k++)
                            {
                                temp += ds.Tables[0].Rows[0]["TABLE_NAME"] + ",";
                            }
                            tbs = temp.Split(",", StringSplitOptions.RemoveEmptyEntries);
                        }
                    }

                    conn.Close();
                }
            }
            catch (Exception ex)
            {
                Loger.Error(this.GetType(), String.Format("检测分表名失败，表名:{0}", splitTableName), ex);
                throw ex;
            }

            for (int i = 0; i < tbs.Length; i++)
            {
                if (tbs[i].Equals(splitTableName))
                {
                    found = true;
                    break;
                }
            }

            return found;
        }

        /// <summary>
        /// 以源表名自动创建归档库的分表
        /// </summary>
        /// <param name="splitTableName">预定义的分表名</param>
        /// <param name="sourceTableName">源表名</param>
        /// <returns></returns>
        public bool CreateSplitTable(string splitTableName, string sourceTableName)
        {
            bool done = false;

            //不能简单的执行create like语句，很有可能归档库是另一个服务器
            //考虑 show create table 语句从源库取建表sql，在归档库执行
            string getCreateSql = String.Format(cmd_txt_show_create_tb, sourceTableName);
            string createTableSql = String.Empty;
            DataSet createSqlData = null;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(m_ConnDestStr))
                {
                    conn.Open();

                    //获取数据表生成SQL脚本
                    createSqlData = MySqlHelper.ExecuteDataset(conn, getCreateSql);
                    if (createSqlData != null && createSqlData.Tables.Count > 0)
                    {
                        createTableSql = createSqlData.Tables[0].Rows[0][1].ToString();
                        createTableSql = createTableSql.Replace("`" + sourceTableName + "`", "");
                        createTableSql = createTableSql.Replace("CREATE TABLE", "");
                    }
                    else
                    {
                        throw new Exception("can't read table DDL scripts from database,table name:" + sourceTableName);
                    }

                    var sql = String.Format(cmd_txt_create_split_tb, splitTableName) + createTableSql;
                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = sql;
                    Loger.Info(this.GetType(), "开始创建分表:\r\n" + sql);
                    int r = cmd.ExecuteNonQuery();
                    done = r == 0;
                    conn.Close();
                    Loger.Info(this.GetType(), "成功创建分表:" + splitTableName);
                }
            }
            catch (Exception ex)
            {
                Loger.Error(this.GetType(), String.Format("创建分表失败，表名:{0}", splitTableName), ex);
                throw ex;
            }
            return done;
        }

        /// <summary>
        /// 从源表复制数据
        /// </summary>
        /// <param name="splitTableName">归档的分表名</param>
        /// <param name="sourceTableName">源表名</param>
        /// <returns></returns>
        public long CopyData(string splitTableName, string sourceTableName)
        {
            long total = 0;
            //找出上一次最后复制的key 
            string lastKey = GetLastKeyValue(splitTableName);
            try
            {
                using (MySqlConnection source = new MySqlConnection(m_ConnSourceStr))
                {
                    source.Open();

                    //读取数据流
                    string sql = String.Format(cmd_txt_read_rows, sourceTableName, m_KeyName, lastKey, m_MaxCopy);
                    MySqlDataReader reader = MySqlHelper.ExecuteReader(source, sql);

                    //复制数据到归档表
                    using (MySqlConnection dest = new MySqlConnection(m_ConnDestStr))
                    {
                        dest.Open();
                        if (reader.HasRows)
                        {
                            string insert = "insert into `{0}` ({1}) values({2})";
                            int cols = reader.FieldCount;
                            while (reader.Read())
                            {
                                string fields = "";
                                string values = "";
                                List<MySqlParameter> parameters = new List<MySqlParameter>();
                                for (int f = 0; f < cols; f++)
                                {
                                    fields += "`" + reader.GetName(f) + "`";
                                    values += "@" + reader.GetName(f);
                                    var v = new MySqlParameter("@" + reader.GetName(f), reader.GetValue(f));
                                    parameters.Add(v);
                                    if (f != cols - 1)
                                    {
                                        fields += ",";
                                        values += ",";
                                    }
                                }

                                insert = String.Format(insert, splitTableName, fields, values);
                                MySqlHelper.ExecuteNonQuery(dest, insert, parameters.ToArray());
                                total += 1;
                            }
                            reader.Close();
                        }
                        dest.Close();
                    }
                    source.Close();
                }
            }
            catch (Exception ex)
            {
                Loger.Error(this.GetType(), String.Format("复制数据失败，分表名:{0}，源表名:{1}", splitTableName, m_TableName), ex);
                throw ex;
            }

            return total;
        }

        /// <summary>
        /// 从源表清理过期数据
        /// </summary>
        /// <param name="splitTableName">分表名</param>
        /// <param name="sourceTableName">源表名</param>
        /// <returns></returns>
        public long DeleteData(string splitTableName, string sourceTableName)
        {
            long total = 0;
            //找出上一次最后复制的key 
            string lastKey = GetLastKeyValue(splitTableName);
            string delete = String.Format(cmd_txt_delete_rows, m_TableName, m_KeyName, lastKey, m_DateField, m_DataHoldDays);
            try
            {
                using (MySqlConnection source = new MySqlConnection(m_ConnSourceStr))
                {
                    source.Open();
                    total = MySqlHelper.ExecuteNonQuery(source, delete);
                    source.Close();
                }
            }
            catch (Exception ex)
            {
                Loger.Error(this.GetType(), String.Format("清理过期数据失败，分表名:{0}，源表名:{1}，数据保留天数:{2}", splitTableName, m_TableName, m_DataHoldDays), ex);
                throw ex;
            }
            return total;
        }

        /// <summary>
        /// 取得最后复制的主键值
        /// </summary>
        /// <param name="splitTableName">分表名</param>
        /// <returns></returns>
        public string GetLastKeyValue(string splitTableName)
        {
            string lastKey = "0";
            //找出上一次最后复制的key
            try
            {
                using (MySqlConnection dest = new MySqlConnection(m_ConnDestStr))
                {
                    dest.Open();
                    MySqlCommand cmd = dest.CreateCommand();
                    cmd.CommandText = String.Format(cmd_txt_select_dest_last_key, m_KeyName, splitTableName);
                    var kv = cmd.ExecuteScalar();
                    if (kv != null)
                    {
                        lastKey = kv.ToString();
                    }

                    dest.Close();
                }
            }
            catch (Exception ex)
            {
                Loger.Error(this.GetType(), String.Format("查询最后复制的主键值失败，分表名:{0}，源表名:{1}，键名:{2}", splitTableName, m_TableName, m_KeyName), ex);
                throw ex;
            }

            return lastKey;
        }
    }
}
