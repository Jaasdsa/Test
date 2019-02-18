using CityPublicClassLib;
using CityUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace CityIoTServiceManager
{
    public class IotCommandParam
    {
        public string commandIP;
        public int port;
        public int timeoutSeconds;
        public string errMsg;

        public bool Check(out string err)
        {
            err = "";
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                err = errMsg;
                return false;
            }
            if (string.IsNullOrWhiteSpace(commandIP))
            {
                err = "配置文件物联控制主机不是有效的IP地址";
                return false;
            }
            if (!IPTool.PingIP(commandIP))
            {
                err = "配置文件物联控制主机网络不通";
                return false;
            }
            if (port == 0)
            {
                err = "配置文件物联控制主机端口不能为0";
                return false;
            }
            if (IPTool.IsValidPort(port))
            {
                err = "配置文件物联控制主机端口为空闲端口，请检查物联网服务是否启动";
                return false;
            }
            if (timeoutSeconds == 0)
            {
                err = "配置文件物联控制主机缺少服务超时时间数配置";
                return false;
            }
            if (string.IsNullOrWhiteSpace(DBUtil.dbConnectString))
            {
                err = "数据库连接字符串未配置";
                return false;
            }
            return true;
        }
    }
    public class DeviceControl
    {
        // 物联网控制参数，单实例加载复制每次调取服务的时候在读配置文件
        private static IotCommandParam iotParam= LoadConfigPath();   
        private static IotCommandParam LoadConfigPath()
        {
            string solutionFilePath = EnvInfo.solutionFilePathForIIS;
            // 加载产品配置文件信息
            IotCommandParam param = new IotCommandParam();
            // 选择加载的需要项目名称
            if (!XMLHelper.LoadSolutionInfo(solutionFilePath, out string solutionName, out string errMsg))
            {
                return new IotCommandParam() { errMsg = "缺少解决方案配置文件，文件路径:" + solutionFilePath };
            }
            // 找到该项目配置文件
            if (!XMLHelper.LoadProjectConfigPath(solutionFilePath, solutionName, out string projectConfigPath, out errMsg))
            {
                return new IotCommandParam() { errMsg = "缺少项目配置文件，文件路径:" + solutionFilePath };
            }
            XmlDocument doc = new XmlDocument();
            if (!XMLHelper.LoadDoc(projectConfigPath, out doc, out  errMsg))
            {
                return new IotCommandParam() { errMsg = "加载项目配置文件失败，文件路径:" + projectConfigPath };
            }
            if (!XMLHelper.ExistsNode(doc, "service/CityIoTService/iotCommand", out XmlNode node, out errMsg))
            {
                return new IotCommandParam() { errMsg = errMsg };
            }
            if (!XMLHelper.LoadStringNode(node, "ip", out param.commandIP, out errMsg))
            {
                return new IotCommandParam() { errMsg = errMsg };
            }
            if (!string.IsNullOrWhiteSpace(param.commandIP) && param.commandIP.ToUpper() == "ANY")// 本地IP地址调整一次
            {
                param.commandIP = "127.0.0.1";
            }
            if (!XMLHelper.LoadNumNode(node, "port", out param.port, out errMsg))
            {
                return new IotCommandParam() { errMsg = errMsg };
            }
            if (!XMLHelper.LoadNumNode(node, "timeoutSeconds", out param.timeoutSeconds, out errMsg))
                return new IotCommandParam() { errMsg = errMsg };

            // 动态实例化数据库的工具类
            DBUtil.InstanceDBStr(projectConfigPath, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
                return new IotCommandParam() { errMsg = errMsg };

            return param;
        }

        // 控制请求接口
        public string WriteJZValue(int userID, int jzID, string fDBAddress, double value, out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            // 服务器参数检查
            if (!iotParam.Check(out errMsg))
            {
                statusCode = "401";
                return "";
            }
            // 控制参数检查
            if (userID == 0)
            {
                errMsg = "用户ID不能为空";
                statusCode = "402";
                return "";
            }
            if (!CheckControlRole(userID, out errMsg))
            {
                statusCode = "430";
                return "";
            }
            if (jzID == 0)
            {
                errMsg = "机组ID不能为空";
                statusCode = "403";
                return "";
            }
            if (string.IsNullOrWhiteSpace(fDBAddress))
            {
                errMsg = "数据业务地址不能为空";
                statusCode = "404";
                return "";
            }
            // 加载机组通信模式
            CommandServerType commandServerType = GetJZCommandServerType(jzID, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                statusCode = "405";
                return "";
            }
            RequestCommand request = new RequestCommand()
            {
                ID=Guid.NewGuid().ToString(),
                sonServerType= commandServerType,
                operType= CommandOperType.Write,
                userID = userID,
                jzID=jzID,
                fDBAddress=fDBAddress,
                value=value,
                state= CommandState.Pending,
                beginTime=DateTime.Now
            };

            return SendRequest(request, out statusCode, out errMsg);
        }
        public string WriteSensorValue(int userID, string sensorID, double value, out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";
            // 服务器参数检查
            if (!iotParam.Check(out errMsg))
            {
                statusCode = "401";
                return "";
            }
            // 控制参数检查
            if (userID == 0)
            {
                errMsg = "用户ID不能为空";
                statusCode = "402";
                return "";
            }
            if (!CheckControlRole(userID, out errMsg))
            {
                statusCode = "430";
                return "";
            }
            if (string.IsNullOrWhiteSpace(sensorID))
            {
                errMsg = "sensorID不能为空";
                statusCode = "403";
                return "";
            }
            // 加载机组通信模式
            CommandServerType commandServerType = GetStationCommandServerType(sensorID, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                statusCode = "405";
                return "";
            }
            RequestCommand request = new RequestCommand()
            {
                ID = Guid.NewGuid().ToString(),
                sonServerType = commandServerType,
                operType = CommandOperType.Write,
                userID = userID,
                sensorID = sensorID,
                value = value,
                state = CommandState.Pending,
                beginTime = DateTime.Now
            };
            return SendRequest(request, out statusCode, out errMsg);
        }

        // 重载数据请求
        public string ReLoadJZData(out string statusCode, out string errMsg)
        {
            // *****************有业务bug要被废弃的****************************
            statusCode = "0000";
            errMsg = "";
            // 服务器参数检查
            if (!iotParam.Check(out errMsg))
            {
                statusCode = "401";
                return "";
            }
            RequestCommand request = new RequestCommand()
            {
                ID = Guid.NewGuid().ToString(),
                sonServerType = CommandServerType.Pump_OPC,
                operType = CommandOperType.ReLoadData,
                state = CommandState.Pending,
                beginTime = DateTime.Now
            };

            return SendRequest(request, out statusCode, out errMsg);
        }
        public string ReLoadJZData(int jzID,out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";
            // 服务器参数检查
            if (!iotParam.Check(out errMsg))
            {
                statusCode = "401";
                return "";
            }
            CommandServerType type = getJZCommmandServerType(jzID, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                statusCode = "402";
                return "";
            }
            RequestCommand request = new RequestCommand()
            {
                ID = Guid.NewGuid().ToString(),
                sonServerType = type,
                operType = CommandOperType.ReLoadData,
                state = CommandState.Pending,
                beginTime = DateTime.Now
            };

            return SendRequest(request, out statusCode, out errMsg);
        }
        public string ReLoadStationData(int stationID, out string statusCode, out string errMsg)  
        {
            statusCode = "0000";
            errMsg = "";
            // 服务器参数检查
            if (!iotParam.Check(out errMsg))
            {
                statusCode = "401";
                return "";
            }
            CommandServerType type = getStationCommmandServerType(stationID, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                statusCode = "402";
                return "";
            }
            RequestCommand request = new RequestCommand()
            {
                ID = Guid.NewGuid().ToString(),
                sonServerType = type,
                operType = CommandOperType.ReLoadData,
                state = CommandState.Pending,
                beginTime = DateTime.Now
            };

            return SendRequest(request, out statusCode, out errMsg);
        }

        // 控制请求实现
        private bool responseFlag = false;
        private byte[] reciveBuffer;
        private string SendRequest(RequestCommand request, out string statusCode, out string errMsg)
        {
            request.timeoutSeconds = iotParam.timeoutSeconds;  //追加超时参数

           SuperSocketTCPClientOper tcpClientOper = new SuperSocketTCPClientOper(iotParam.commandIP, iotParam.port);
            tcpClientOper.DataReceived += WriteResponseReceiveHandle;
            if (!tcpClientOper.Connect(out string clienName, out errMsg))
            {
                errMsg = "连接物联网服务失败,请检查服务服务器是否打开:" + errMsg;
                statusCode = "410";
                return "";
            }
            string jsonStr = ByteUtil.AddLineBreak(ByteUtil.ToSerializeObject(request));
            // 实现内部协议 GUID-key，T-body
            string key = Guid.NewGuid().ToString();
            string data = key + " " + jsonStr;
            byte[] sendBuffer = ByteUtil.ToSerializeBuffer(data);
            tcpClientOper.SendData(clienName, sendBuffer, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                errMsg = "请求物联网服务数据包发送失败:" + errMsg;
                statusCode = "412";
                return "";
            }

            // 主线程等待设定秒 不返回认定为超时
            SpinWait wait = new SpinWait();
            DateTime dt = DateTime.Now;
            while ((DateTime.Now - dt) < TimeSpan.FromSeconds(iotParam.timeoutSeconds + 4))
            {
                if (this.responseFlag)
                    break;
                wait.SpinOnce();
            }
            if (this.responseFlag) // 服务成功返回   
            {
                try
                {
                    ResponseCommand response = ByteUtil.ToDeserializeObject<ResponseCommand>(reciveBuffer);
                    statusCode = response.statusCode;
                    errMsg = response.errMsg;
                    return response.info;
                }
                catch(Exception e)
                {
                    statusCode = "413";
                    errMsg = "物联网服务返回参数反序列化失败："+e.Message;
                    return "";
                }
            }
            else
            {
                statusCode = "414";
                errMsg = "服务超时未返回";
                return "";
            }
        }
        private void WriteResponseReceiveHandle(object sender, byte[] data)
        {
            this.responseFlag = true;
            this.reciveBuffer = data;
        }

        // 检查用户控制权限
        private bool CheckControlRole(int userID, out string errMsg)
        {
            errMsg = "";
            string sql = @"select * from FLOW_USER_ROLE where 机构ID in (select 机构ID from FLOW_GROUPS where 名称='泵房操作员');";
            DataTable dt = DBUtil.ExecuteDataTable(sql, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                errMsg = "检查用户权限失败:" + errMsg;
                return false;
            }
            foreach(DataRow dr in dt.Rows)
            {
                int curUserID = DataUtil.ToInt(dr["用户ID"]);
                if (curUserID == userID)
                    return true;
            }
            errMsg = "该用户不具备泵房操作员权限";
            return false;
        }

        // 查询通信模式
        private CommandServerType GetJZCommandServerType(int jzID,out string errMsg)
        {
            errMsg = "";
            string sql = string.Format(@"select top 1 t1.PumpJZReadMode from Pump t,PumpJZ t1 where t.ID=t1.PumpId and (t.是否删除=0 or t.是否删除 is null) and (t1.是否删除=0 or t1.是否删除 is null) and t1.ID={0}", jzID);
            string jzReadMode =DataUtil.ToString(DBUtil.ExecuteScalar(sql, out  errMsg)).Trim();
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                errMsg = "查询机组通信模式失败" + errMsg;
                return CommandServerType.UnKnown;
            }
            if (string.IsNullOrWhiteSpace(jzReadMode))
            {
                errMsg="机组ID：" + jzID + " 读取模式字段值异常或机组已删除，请检查数据库数据";
                return CommandServerType.UnKnown;
            }
            if (jzReadMode == "OPC")
                return CommandServerType.Pump_OPC;
            else if (jzReadMode == "WEB-PUMP")
                return CommandServerType.Pump_WEB;
            else
            {
                errMsg = "机组ID：" + jzID + " 读取模式为未知读取模式，请检查数据库数据";
                return CommandServerType.UnKnown;
            }
        }
        private CommandServerType GetStationCommandServerType(string sensorID, out string errMsg)
        {
            errMsg = "";
            string sql = string.Format(@"select top 1  t.ReadMode from SCADA_Station t,SCADA_Sensor t1
                                         where t.ID=t1.StationID and (t1.是否删除=0 or t1.是否删除 is null) and (t.是否删除=0 or t.是否删除 is null)  and t1.ID='{0}'", sensorID);
            string jzReadMode = DataUtil.ToString(DBUtil.ExecuteScalar(sql, out errMsg)).Trim();
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                errMsg = "查询站点通信模式失败" + errMsg;
                return CommandServerType.UnKnown;
            }
            if (string.IsNullOrWhiteSpace(jzReadMode))
            {
                errMsg = "sensorID：" + sensorID + " 读取模式字段值异常，请检查数据库数据";
                return CommandServerType.UnKnown;
            }
            if (jzReadMode == "OPC")
                return CommandServerType.SCADA_OPC;
            else if (jzReadMode == "WEB-ZHCD")
                return CommandServerType.ZHCD_WEB;
            else if (jzReadMode == "WEB-YL")
                return CommandServerType.YL_WEB;
            else
            {
                errMsg = "sensorID：" + sensorID + " 读取模式为未知读取模式，请检查数据库数据";
                return CommandServerType.UnKnown;
            }
        }

        // 根据机组ID得到子服务类型
        private CommandServerType getJZCommmandServerType(int jzID, out string errMsg)
        {
            CommandServerType type = CommandServerType.UnKnown;
            errMsg = "";

            if (jzID == 0)
            {
                errMsg = "机组ID"+jzID+"为空";
                return type;
            }
            string sql = string.Format(@"select top 1 t1.PumpJZReadMode from Pump t,PumpJZ t1 where t.ID=t1.PumpId and (t.是否删除=0 or t.是否删除 is null) and (t1.是否删除=0 or t1.是否删除 is null) and t1.ID={0}", jzID);
            object val = DBUtil.ExecuteScalar(sql, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
                return type;
            return GetJZCommandServerType(val.ToString());
        }
        private CommandServerType getStationCommmandServerType(int stationID, out string errMsg) 
        {
            CommandServerType type = CommandServerType.UnKnown;
            errMsg = "";

            if (stationID == 0)
            {
                errMsg = "站点ID" + stationID + "为空";
                return type;
            }
            string sql = string.Format(@"select top 1 t.ReadMode from SCADA_Station t where t.ID={0}", stationID);
            object val = DBUtil.ExecuteScalar(sql, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
                return type;
            return GetStationCommandServerType(val.ToString());
        }
        public  CommandServerType GetJZCommandServerType(string pumpJZReadMode)
        {
            if (string.IsNullOrWhiteSpace(pumpJZReadMode))
                return CommandServerType.UnKnown;
            if (pumpJZReadMode == "OPC")
                return CommandServerType.Pump_OPC;
            else if (pumpJZReadMode == "WEB-PUMP")
                return CommandServerType.Pump_WEB;
            else
                return CommandServerType.UnKnown;
        }
        public  CommandServerType GetStationCommandServerType(string stationReadMode)
        {
            if (string.IsNullOrWhiteSpace(stationReadMode))
                return CommandServerType.UnKnown;
            if (stationReadMode == "OPC")
                return CommandServerType.SCADA_OPC;
            else if (stationReadMode == "WEB-ZHCD")
                return CommandServerType.ZHCD_WEB;
            else if (stationReadMode == "WEB-YL")
                return CommandServerType.YL_WEB;
            else
                return CommandServerType.UnKnown;
        }
    }
}
