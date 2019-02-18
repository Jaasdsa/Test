using CityServer.Lawer;
using CityUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Xml;

namespace CityIoTServiceManager
{
    public class ServiceManager
    {
        public ServiceManager(EnvType type)
        {
            this.type = type;
            this.LoadOnceXMLInfo();
        }

        // 环境信息
        EnvType type;
        static bool EnvIsOkay { get; set; } = false;
        static string ErrorPolicy { get; set; }

        // 服务名称
        public static string serverName;
        static string dispatchServerName;
        // 服务描述
        public static string serverDescribe;
        static string dispatchServerDescribe; 

        // 解决方案文件路径
        static string solutionFilePath;
        // 服务文件路径
        public static string iotServerPath;
        static string dispatchIotServerPath; 
        // 项目配置文件路径
        static string projectConfigPath;
        public static bool IsNeedOPCServer;

        #region 框架测试服务

        /// <summary>
        /// 框架测试服务
        /// </summary>
        /// <param name="test">测试参数</param>
        /// <param name="statusCode">服务返回状态码，成功为"0000",失败为其他</param>
        /// <param name="errMsg">服务错误返回参数，成功返回"",失败返回错误字符串</param>
        /// <returns>服务返回结果</returns>
        public string Test(string test, out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";
            string data ="";
            /**********逻辑代码**********/
            Thread.Sleep(1000);

            return DateTime.Now.ToString();
        }

        #endregion

        #region 服务管理

        // 注册服务--核心服务和调度服务
        public string IsInstalCoreHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            if (!File.Exists(iotServerPath))
            {
                errMsg = serverName + "注册失败:未找到服务文件" + iotServerPath;
                statusCode = "4004";
                return "";
            }
            if (ServiceToolEx.IsServiceExist(serverName))
            {
                errMsg= serverName + "服务已存在";
                statusCode = "4000";
                return "";
            }

            ServiceController service = ServiceToolEx.InstallService(iotServerPath, serverName, serverName,serverDescribe, System.ServiceProcess.ServiceStartMode.Manual);
            if (service == null)
            {
                errMsg=serverName + "服务注册失败";
                statusCode = "4001";
                return "";
            }
            if (!ServiceToolEx.IsServiceExist(serverName))
            {
                errMsg = serverName + "服务注册失败!"+ErrorPolicy;
                statusCode = "4005";
                return "";
            }
            string dispatchInfo = IsInstalDispatchHander(out statusCode, out errMsg);
            return serverName + "服务注册成功;"+dispatchInfo ;
        }
        private string IsInstalDispatchHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            if (!File.Exists(dispatchIotServerPath))
            {
                errMsg = dispatchServerName + "注册失败:未找到服务文件" + dispatchIotServerPath;
                statusCode = "4104";
                return "";
            }
            if (ServiceToolEx.IsServiceExist(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务已存在";
                statusCode = "4100";
                return "";
            }
            ServiceController service = ServiceToolEx.InstallService(dispatchIotServerPath, dispatchServerName, dispatchServerName, dispatchServerDescribe, System.ServiceProcess.ServiceStartMode.Manual);
            if (service == null)
            {
                errMsg = dispatchServerName + "服务注册失败";
                statusCode = "4101";
                return "";
            }
            if (!ServiceToolEx.IsServiceExist(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务注册失败!" + ErrorPolicy;
                statusCode = "4105";
                return "";
            }
            return dispatchServerName + "服务注册成功";
        }
        // 卸载服务--核心服务和调度服务
        public string IsUnInstalCoreHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            if (!ServiceToolEx.IsServiceExist(serverName))
            {
                errMsg=serverName + "服务不存在";
                statusCode = "4000";
                return "";
            }
            if (!ServiceToolEx.UninstallService(serverName))
            {
                errMsg=serverName + "服务卸载失败!" + ErrorPolicy;
                statusCode = "4001";
                return "";
            }
            string dispatchInfo = IsUnInstalDispatchHander(out statusCode, out errMsg);
            return serverName + "服务卸载成功;" + dispatchInfo;
        }
        public string IsUnInstalDispatchHander(out string statusCode, out string errMsg) 
        {
            statusCode = "0000";
            errMsg = "";

            if (!ServiceToolEx.IsServiceExist(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务不存在";
                statusCode = "4100";
                return "";
            }
            if (!ServiceToolEx.UninstallService(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务卸载失败!" + ErrorPolicy;
                statusCode = "4101";
                return "";
            }
            return dispatchServerName + "服务卸载成功";
        }
        // 启动服务
        public string IsStartCoreHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";
            if (!ServiceToolEx.IsServiceExist(serverName))
            {
                errMsg = serverName + "服务不存在";
                statusCode = "4004";
                return "";
            }
            string curFilePath = ServiceToolEx.GetWindowsServiceInstallPath(serverName);
            if(curFilePath.ToUpper() != new DirectoryInfo(iotServerPath).Parent.FullName.ToUpper())
            {
                errMsg = serverName + "服务路径不一致,请先卸载原来路径的服务:"+ curFilePath;
                statusCode = "4007";
                return "";
            }
            if (ServiceToolEx.IsServiceRunning(serverName))
            {
                errMsg = serverName + "服务已经在运行";
                statusCode = "4000";
                return "";
            }
            ServiceController service = ServiceToolEx.StartService(serverName, new string[0] );
            if (service == null)
            {
                errMsg = serverName + "服务启动失败";
                statusCode = "4001";
            }
            if (!ServiceToolEx.IsServiceRunning(serverName))
            {
                errMsg = serverName + "服务启动失败!" + ErrorPolicy;
                statusCode = "4005";
                return "";
            }
            string dispatchInfo = IsStartDispatchHander(out statusCode, out errMsg);
            return serverName + "服务已启动;" + dispatchInfo;
        }
        public string IsStartDispatchHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";
            if (!ServiceToolEx.IsServiceExist(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务不存在";
                statusCode = "4004";
                return "";
            }
            string curFilePath = ServiceToolEx.GetWindowsServiceInstallPath(dispatchServerName);
            if (curFilePath.ToUpper() != new DirectoryInfo(dispatchIotServerPath).Parent.FullName.ToUpper())
            {
                errMsg = dispatchServerName + "服务路径不一致,请先卸载原来路径的服务:" + curFilePath;
                statusCode = "4007";
                return "";
            }
            if (ServiceToolEx.IsServiceRunning(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务已经在运行";
                statusCode = "4000";
                return "";
            }
            ServiceController service = ServiceToolEx.StartService(dispatchServerName, new string[0]);
            if (service == null)
            {
                errMsg = dispatchServerName + "服务启动失败";
                statusCode = "4001";
            }
            if (ServiceToolEx.IsServiceRunning(dispatchServerName))
                return dispatchServerName + "服务已启动";
            else
            {
                errMsg = dispatchServerName + "服务启动失败!" + ErrorPolicy;
                statusCode = "4005";
                return "";
            }

        }
        // 停止服务
        public string IsStopCoreHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            if (!ServiceToolEx.IsServiceRunning(serverName))
            {
                errMsg = serverName + "服务未运行";
                statusCode = "4000";
                return "";
            }
            ServiceController service = ServiceToolEx.StopService(serverName);
            if (service == null)
            {
                errMsg = serverName + "服务停止失败";
                statusCode = "4001";
                return "";
            }
            if (ServiceToolEx.IsServiceRunning(serverName))
            {
                errMsg = serverName + "服务停止失败!" + ErrorPolicy;
                statusCode = "4005";
                return "";
            }
            string dispatchInfo = IsStopDispatchHander(out statusCode, out errMsg);
            return serverName + "服务已停止;" + dispatchInfo;
        }
        public string IsStopDispatchHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            if (!ServiceToolEx.IsServiceRunning(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务未运行";
                statusCode = "4000";
                return "";
            }
            ServiceController service = ServiceToolEx.StopService(dispatchServerName);
            if (service == null)
            {
                errMsg = dispatchServerName + "服务停止失败";
                statusCode = "4001";
                return "";
            }
            if (!ServiceToolEx.IsServiceRunning(dispatchServerName))
                return dispatchServerName + "服务已停止";
            else
            {
                errMsg = dispatchServerName + "服务停止失败!" + ErrorPolicy;
                statusCode = "4005";
                return "";
            }
        }
        // 重启服务
        public string IsRestartCoreHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            if (!ServiceToolEx.IsServiceExist(serverName))
            {
                errMsg = serverName + "服务不存在请先注册";
                statusCode = "4000";
                return "";
            }
            ServiceController service;
            if (ServiceToolEx.IsServiceRunning(serverName))
            {
                service = ServiceToolEx.StopService(serverName);
                if (service == null)
                {
                    service = null;
                    errMsg = serverName + "服务停止失败";
                    statusCode = "4001";
                    return "";
                }
                if (ServiceToolEx.IsServiceRunning(serverName))
                {
                    errMsg = serverName + "服务停止失败!" + ErrorPolicy;
                    statusCode = "4005";
                    return "";
                }
            }
            service = ServiceToolEx.StartService(serverName,new string[0]);
            if (service == null)
            {
                errMsg = serverName + "服务启动失败";
                statusCode = "4002";
                return "";
            }
            if (!ServiceToolEx.IsServiceRunning(serverName))
            {
                errMsg = serverName + "服务启动失败!" + ErrorPolicy;
                statusCode = "4005";
                return "";
            }
            string dispatchInfo = IsRestartDispatchHander(out statusCode, out errMsg);
            return serverName + "服务已重新启动;" + dispatchInfo;
        }
        public string IsRestartDispatchHander(out string statusCode, out string errMsg)
        {
            statusCode = "0000";
            errMsg = "";

            if (!ServiceToolEx.IsServiceExist(dispatchServerName))
            {
                errMsg = dispatchServerName + "服务不存在请先注册";
                statusCode = "4000";
                return "";
            }
            ServiceController service;
            if (ServiceToolEx.IsServiceRunning(dispatchServerName))
            {
                service = ServiceToolEx.StopService(dispatchServerName);
                if (service == null)
                {
                    service = null;
                    errMsg = dispatchServerName + "服务停止失败";
                    statusCode = "4001";
                    return "";
                }
                if (ServiceToolEx.IsServiceRunning(dispatchServerName))
                {
                    errMsg = dispatchServerName + "服务停止失败!" + ErrorPolicy;
                    statusCode = "4005";
                    return "";
                }
            }
            service = ServiceToolEx.StartService(dispatchServerName, new string[0]);
            if (service == null)
            {
                errMsg = dispatchServerName + "服务启动失败";
                statusCode = "4002";
                return "";
            }
            if (ServiceToolEx.IsServiceRunning(dispatchServerName))
                return serverName + "服务已重新启动";
            else
            {
                errMsg = dispatchServerName + "服务启动失败!" + ErrorPolicy;
                statusCode = "4005";
                return "";
            }
        }
        // 服务是否存在
        public bool IsCoreServiceExist()
        {
            return ServiceToolEx.IsServiceExist(serverName);
        }
        public bool IsDispatchServiceExist()
        {
            return ServiceToolEx.IsServiceExist(dispatchServerName);
        }
        // 服务是否在运行
        public bool IsCoreServiceRun()
        {
            if (!ServiceToolEx.IsServiceExist(serverName))
                return false;
            return ServiceToolEx.IsServiceRunning(serverName);
        }
        public bool IsDispatchServiceRun() 
        {
            if (!ServiceToolEx.IsServiceExist(dispatchServerName))
                return false;
            return ServiceToolEx.IsServiceRunning(dispatchServerName);
        }

        //切换解决方案
        public List<string> GetConfCenterList()
        {
            List<string> confCenterList = new List<string>();
            string configFilePath = new DirectoryInfo(solutionFilePath).Parent.FullName + @"\项目\";
            DirectoryInfo theFolder = new DirectoryInfo(configFilePath);
            DirectoryInfo[] dirInfo = theFolder.GetDirectories();
            //遍历文件夹
            foreach (DirectoryInfo NextFolder in dirInfo)
            {
                confCenterList.Add(NextFolder.Name);
            }
            return confCenterList;
        }
        public bool ChangeConfCenter(string solutionfolderName,out string errMsg)
        {
            XmlDocument doc = new XmlDocument();
            if (string.IsNullOrWhiteSpace(solutionFilePath))
            {
                errMsg = "框架配置文件路径未指定明确";
                return false;
            }
            if (!XMLHelper.LoadDoc(solutionFilePath, out doc, out errMsg))
            {
                errMsg = "配置文件获取失败：" + errMsg;
                return false;
            }
            if (!XMLHelper.ExistsNode(doc, "configuration/solutionName", out XmlNode node, out errMsg))
            {
                errMsg = "获取配置文件项目节点失败：" + errMsg;
                return false;
            }
            if (string.IsNullOrWhiteSpace(solutionfolderName))
            {
                errMsg = "没有配置当前解决方案名称";
                return false;
            }
            node.InnerText = solutionfolderName;            
            doc.Save(solutionFilePath);

            LoadXMLInfo(); //切换解决方案后一定要重新加载一次配置
            return true;
        }
        // 启动前环境检查
        public bool EnvCheckForBeforeRun(out string projectConfigPath, out string errMsg) 
        {
            errMsg = "";
            projectConfigPath = "";
            // 选择加载的需要项目名称
            if (!XMLHelper.LoadSolutionInfo(solutionFilePath, out string solutionName, out errMsg))
                return false;
            // 找到该项目配置文件
            if (!XMLHelper.LoadProjectConfigPath(solutionFilePath, solutionName, out  projectConfigPath, out errMsg))
                return false;
            // 找到日志的端口
            if (!XMLHelper.LoadLogPort(projectConfigPath, out int logPort, out errMsg))
                return false;
            // 判断日志端口
            if (!IPTool.IsValidPort(logPort))
            {
                errMsg = "日志服务器的发布端口被占用，请更换端口号,端口:" + logPort;
                return false;
            }
            return true;
        }
        private void LoadOnceXMLInfo()
        {
            if (EnvIsOkay)
                return ;  //环境正常不要每次读配置，从缓存取就行了
            LoadXMLInfo();
    }
        private void LoadXMLInfo()
        {
            switch (this.type)
            {
                case EnvType.IIS:
                    {
                        solutionFilePath = EnvInfo.solutionFilePathForIIS;
                        iotServerPath = EnvInfo.iotServerFilePathForIIS;
                        dispatchIotServerPath = EnvInfo.iotDispatchServerFilePathForIIS;
                        ErrorPolicy = "请检查IIS应用程序池“标识”是否设置为“LocalSystem”权限器。";
                    }
                    break;
                case EnvType.CS:
                    {
                        solutionFilePath = EnvInfo.solutionFilePathForCS;
                        iotServerPath = EnvInfo.iotServerFilePathForCS;
                        dispatchIotServerPath = EnvInfo.iotDispatchServerFilePathForCS;
                        ErrorPolicy = "请检查程序是否以管理员方式启动";
                    }
                    break;
                default:
                    {
                        ErrorPolicy = "未知服务宿主类型";
                        EnvIsOkay = false;
                        return;
                    }
            }
            // 选择加载的需要项目名称
            if (!XMLHelper.LoadSolutionInfo(solutionFilePath, out string solutionName, out string errMsg))
            {
                ErrorPolicy = errMsg;
                EnvIsOkay = false;
                return;
            }
            // 找到该项目配置文件
            if (!XMLHelper.LoadProjectConfigPath(solutionFilePath, solutionName, out projectConfigPath, out errMsg))
            {
                ErrorPolicy = errMsg;
                EnvIsOkay = false;
                return;
            }
            // 找到该项目配置文件
            if (!XMLHelper.LoadProjectInfo(projectConfigPath, out string projectServerName, out errMsg))
            {
                ErrorPolicy = errMsg;
                EnvIsOkay = false;
                return;
            }
            // 找到是否需要OPCServer
            if (!GetIsNeedOPCServer(out errMsg))
            {
                ErrorPolicy = errMsg;
                EnvIsOkay = false;
                return;
            }
            // 核心服务信息
            serverName = EnvInfo.GetServerName(projectServerName);
            serverDescribe = EnvInfo.GetServerDescribe(projectServerName);
            // 调度服务信息
            dispatchServerName = EnvInfo.GetDispatchServerName(projectServerName);
            dispatchServerDescribe = EnvInfo.GetDispatchServerDescribe(projectServerName);
            // 成功标志置位
            EnvIsOkay = true;
        }

        // 服务信息获取
        public string GetProjectConfigPath()
        {
            return projectConfigPath;
        }

        // 杀掉KepServer服务，因为它也会挂
        private  bool GetIsNeedOPCServer(out string errMsg)
        {
            errMsg = "";
            XmlDocument doc = new XmlDocument();
            if (!XMLHelper.LoadDoc(projectConfigPath, out doc, out errMsg))
                return false;
            if (!XMLHelper.ExistsNode(doc, "service/RunServerList", out XmlNode node, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "OPCPumpDataService", out int runOPCPumpServiceFlag, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "OPCScadaDataService", out int runOPCScadaServiceFlag, out errMsg))
                return false;
            if (runOPCPumpServiceFlag == 1 || runOPCScadaServiceFlag == 1)
            {
                IsNeedOPCServer = true;
                return true;
            }
            return true; ;
        }
        public bool StopOPCServer(out string errMsg)
        {
            errMsg = "";
            if (!IsNeedOPCServer)
            {
                errMsg = "检测到核心服务不需要OPCServer,停止OPCServer失败";
                return false;
            }
            string[] services = ServiceToolEx.GetLocalHostServerList();
            List<string> kepServices = new List<string>();
            foreach(string servicesName in services)
            {
                if (servicesName.ToUpper().Contains("KEPSERVEREX"))
                    kepServices.Add(servicesName);
            }
            if (kepServices.Count == 0)
            {
                errMsg = "检测到本地计算机没有安装KepServer,无法停止其服务";
                return false;
            }
            errMsg = "";
            foreach (string servicesName in kepServices)
            {
                if (!ServiceToolEx.StopService(servicesName, out string err))
                    errMsg = errMsg + err;
            }
            if (string.IsNullOrWhiteSpace(errMsg))
                return true;
            return false;
        }
        #endregion
    }
}
