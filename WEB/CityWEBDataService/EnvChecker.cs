using CityUtils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace CityWEBDataService 
{
    class EnvChecker
    {
        //**** 环境检查工作者 *****
        private static string ip;
        private static string serverName;
        private static string user;
        private static string password;
        private static string DBConnectString;
        private static string CreateConnStr()
        {
            DBConnectString = string.Format("server={0};database='{1}';User id={2};password={3};Integrated Security=false", ip, serverName, user, password);
            return DBConnectString;
        }

        private static XmlDocument doc =new XmlDocument();

        // 熊猫二供WEB服务环境环境检查器
        public static bool CheckPandaPumpWEB(out string errMsg)
        {
            errMsg = "";

            if (!XMLHelper.LoadDoc(Config.configFilePath, out doc, out errMsg))
                return false;
            if (!CheckAndCreateConnStr(out errMsg))
                return false;
            if (!IPTool.PingIP(ip))
            {
                errMsg = "业务数据库网络不通，请保证网络通畅！";
                return false;
            }
            if (!checkConnect())
            {
                errMsg = "测试连接业务数据库失败！";
                return false;
            }

            if (!CheckAndCreatePandaPumpParam(out errMsg))
                return false;

            doc = null;

            return true;
        }
        public static bool CheckPandaPumpScadaWEB(out string errMsg) 
        {
            errMsg = "";

            if (!XMLHelper.LoadDoc(Config.configFilePath, out doc, out errMsg))
                return false;
            if (!CheckAndCreateConnStr(out errMsg))
                return false;
            if (!IPTool.PingIP(ip))
            {
                errMsg = "业务数据库网络不通，请保证网络通畅！";
                return false;
            }
            if (!checkConnect())
            {
                errMsg = "测试连接业务数据库失败！";
                return false;
            }

            if (!CheckAndCreatePandaPumpScadaParam(out errMsg))
                return false;

            doc = null;

            return true;
        }
        public static bool CheckPandaYaLiWEB(out string errMsg)
        {
            errMsg = "";

            if (!XMLHelper.LoadDoc(Config.configFilePath, out doc, out errMsg))
                return false;
            if (!CheckAndCreateConnStr(out errMsg))
                return false;
            if (!IPTool.PingIP(ip))
            {
                errMsg = "业务数据库网络不通，请保证网络通畅！";
                return false;
            }
            if (!checkConnect())
            {
                errMsg = "测试连接业务数据库失败！";
                return false;
            }

            if (!CheckAndCreatePandaYaLiParam(out errMsg))
                return false;

            doc = null;

            return true;
        }
        public static bool CheckPandaCeDianWEB(out string errMsg)
        {
            errMsg = "";

            if (!XMLHelper.LoadDoc(Config.configFilePath, out doc, out errMsg))
                return false;
            if (!CheckAndCreateConnStr(out errMsg))
                return false;
            if (!IPTool.PingIP(ip))
            {
                errMsg = "业务数据库网络不通，请保证网络通畅！";
                return false;
            }
            if (!checkConnect())
            {
                errMsg = "测试连接业务数据库失败！";
                return false;
            }

            if (!CheckAndCreatePandaCeDianParam(out errMsg))
                return false;

            doc = null;

            return true;
        }

        // 检查并创造连接字符串
        private static bool CheckAndCreateConnStr(out string errMsg)
        {
            errMsg = "";
            try
            {
                //得到连接字符串节点
                if (!XMLHelper.ExistsNode(doc, "service/connStr", out XmlNode connStrNode, out errMsg))
                    return false;

                ip = connStrNode.Attributes["ip"].Value;
                user = connStrNode.Attributes["user"].Value;
                password = connStrNode.Attributes["password"].Value;
                serverName = connStrNode.Attributes["serverName"].Value;
                CreateConnStr();

                return true;
            }
            catch (Exception e)
            {
                errMsg = e.Message;
                return false;
            }
        }
        // 测试数据库连接
        private static bool checkConnect()
        {
            return DBUtil.GetConnectionTest();
        }

        // 加载熊猫二供WEB服务参数
        private static bool CheckAndCreatePandaPumpParam(out string errMsg) 
        {
            errMsg = "";
            Config.pandaPumpParam = new PandaParam();
            if (!XMLHelper.ExistsNode(doc, "service/WebPandaPumpDataService", out XmlNode node, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appKey", out Config.pandaPumpParam.appKey, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appSecret", out Config.pandaPumpParam.appSecret, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getTokenUrl", out Config.pandaPumpParam.getTokenUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getDataUrl", out Config.pandaPumpParam.getDataUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "useName", out Config.pandaPumpParam.useName, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "collectInterval", out Config.pandaPumpParam.collectInterval, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "saveInterVal", out Config.pandaPumpParam.saveInterVal, out errMsg))
                return false;
            return true;
        }
        // 加载熊猫二供WEB-SCADA服务参数
        private static bool CheckAndCreatePandaPumpScadaParam(out string errMsg) 
        {
            errMsg = "";
            Config.pandaPumpScadaParam = new PandaParam();
            if (!XMLHelper.ExistsNode(doc, "service/WebPandaPumpScadaDataService", out XmlNode node, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appKey", out Config.pandaPumpScadaParam.appKey, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appSecret", out Config.pandaPumpScadaParam.appSecret, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getTokenUrl", out Config.pandaPumpScadaParam.getTokenUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getDataUrl", out Config.pandaPumpScadaParam.getDataUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "useName", out Config.pandaPumpScadaParam.useName, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "collectInterval", out Config.pandaPumpScadaParam.collectInterval, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "saveInterVal", out Config.pandaPumpScadaParam.saveInterVal, out errMsg))
                return false;
            return true;
        }
        // 加载熊猫监测压力点服务参数
        private static bool CheckAndCreatePandaYaLiParam(out string errMsg)
        {
            errMsg = "";
            Config.pandaYaLiParam = new PandaParam();
            if (!XMLHelper.ExistsNode(doc, "service/WebPandaYLDataService", out XmlNode node, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appKey", out Config.pandaYaLiParam.appKey, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appSecret", out Config.pandaYaLiParam.appSecret, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getTokenUrl", out Config.pandaYaLiParam.getTokenUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getDataUrl", out Config.pandaYaLiParam.getDataUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "useName", out Config.pandaYaLiParam.useName, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "collectInterval", out Config.pandaYaLiParam.collectInterval, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "saveInterVal", out Config.pandaYaLiParam.saveInterVal, out errMsg))
                return false;
            return true;
        }
        // 加载熊猫综合测点服务参数
        private static bool CheckAndCreatePandaCeDianParam(out string errMsg) 
        {
            errMsg = "";
            Config.pandaCeDianParam = new PandaParam();
            if (!XMLHelper.ExistsNode(doc, "service/WebPandaZHCDDataService", out XmlNode node, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appKey", out Config.pandaCeDianParam.appKey, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "appSecret", out Config.pandaCeDianParam.appSecret, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getTokenUrl", out Config.pandaCeDianParam.getTokenUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "getDataUrl", out Config.pandaCeDianParam.getDataUrl, out errMsg))
                return false;
            if (!XMLHelper.LoadStringNode(node, "useName", out Config.pandaCeDianParam.useName, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "collectInterval", out Config.pandaCeDianParam.collectInterval, out errMsg))
                return false;
            if (!XMLHelper.LoadNumNode(node, "saveInterVal", out Config.pandaCeDianParam.saveInterVal, out errMsg))
                return false;
            return true;
        }
    }
}
