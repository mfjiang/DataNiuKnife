# DataNiuKnife
数据牛刀是一款提供大数据表自动分割、归档和清理过期数据的服务。它使用C# /.NET CORE实现，当前支持Mysql数据源。

Data Niu Knife is a service provides  to automatically splits, archive, and clean up stale data for big data tables. It is implemented using C# /.NET CORE and currently supports Mysql data sources.

希望使用此服务的大数据表，有如下要求：
 1. 数据表存在自增长ID；
 2. 数据表存在时间列，数据以时间顺序增长；
 3. 数据表不存在外键结束；
 4. 提供一个拥有读取数据表元数据和建表权限的数据库账号；

If you want to use this service be sure following requirements:
 1. the big data table is based on the self-growth ID;
 2. the big data table has time column, the data grows by time order;
 3. the big data table does not have a foreign key at all;
 4. need a DB Account with the authority to read the data table metadata and create the data table;

配置示例：
Configuration Sample:

    {
    "AppSettings": {
        "LogManPath": "/app/LogMan/",        
    },
    "MySqlClusterSettings": {
        "Nodes": [
            {    
               {
                "MysqlNode": {
                    "ID":1,
                    "IsSlave": false,
                    "DataBasesName": "data_sharding_a",
                    "ConnStr": "server=192.168.3.250;database=data_sharding_a;user=app_user;password=your pwd;charset=utf8;",
                    "DevideFromNodeID": 2,
                    "DevideDataSet": "table 1:hash key,table 2:hash key,table n:hash key",
                    "AutoMoveDataSet": "table_name=data_shard,key_name=id,date_field=created,data_hold_days=180,archive_node_id=2,schedule_time=23:00:00;"
                }
            },
            {
                "MysqlNode": {
                    "ID": 2,
                    "IsSlave": false,
                    "DataBasesName": "data_sharding_b",
                    "ConnStr": "server=192.168.3.250;database=data_sharding_b;user=app_user;password=your pwd;charset=utf8;",
                    "DevideFromNodeID": 0,
                    "DevideDataSet": "",
                    "AutoMoveDataSet": ""
                }
            }
        ]
    },
    "AllowedHosts": "*"
    }

理解这一段配置：
Understand this paragraph configuration:

    "AutoMoveDataSet": "table_name=data_shard,key_name=id,date_field=created,data_hold_days=180,archive_node_id=2,schedule_time=23:00:00;"
table_name: 
要列入自动管理的数据表名。
the name of data table that you want to use this service.

key_name: 
主键名。
main key on the data table.

date_field: 
时间列名。
the date time column.

data_hod_days:
数据保留期的天数。
how many days for keep the data before removed.

archive_node_id: 
本配置中作为归档库的数据库节点ID。
the data archive DB node id in this configures.

schedule_time:
每日运行的计划时间。
the time for start jobs per day.


本服务可以在docker容器中运行：
This service supports docker container:

    docker build -f Dockfile -t data_niu_knife:demo .
    docker run --name data_niu_knife_hosted --mount type=bind,source=/home/docker_data/DataNiuKnife/LogMan/,target=/app/LogMan/ -d data_niu_knife:demo .
