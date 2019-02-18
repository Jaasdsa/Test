using CityServer.Lawer;
using CityUtils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;
using System.Threading;

namespace CityIoTServiceManager
{
    public class Person
    {
        public string name;
        public int age;
    }

    [AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
    [ServiceBehavior(ConcurrencyMode = ConcurrencyMode.Multiple, MaxItemsInObjectGraph = 65536000)]
    public partial class REST : IREST
    {
        #region 框架测试服务

        /// <summary>
        /// 接口测试
        /// </summary>
        public Status TestGet(string test)
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            ServiceManager oper = new ServiceManager(EnvType.IIS);
            response.info = oper.Test(test, out statusCode, out errMsg) + "  输入了" + test;
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        public Person TestPost(Person person)
        {
            Thread.Sleep(5000);
            return person;
        }
        #endregion

        #region 服务管理

        /// <summary>
        /// 安装服务
        /// </summary>
        public Status IsInstalService()
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            ServiceManager manager = new ServiceManager(EnvType.IIS);
            response.info = manager.IsInstalCoreHander(out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        /// <summary>
        /// 卸载服务
        /// </summary>
        public Status IsUnInstalService()
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            ServiceManager manager = new ServiceManager(EnvType.IIS);
            response.info = manager.IsUnInstalCoreHander(out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        /// <summary>
        /// 启动服务
        /// </summary>
        public Status IsStartService()
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            ServiceManager manager = new ServiceManager(EnvType.IIS);
            response.info = manager.IsStartCoreHander(out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        /// <summary>
        /// 停止服务
        /// </summary>
        public Status IsStopService()
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            ServiceManager manager = new ServiceManager(EnvType.IIS);
            response.info = manager.IsStopCoreHander(out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        /// <summary>
        /// 重启服务
        /// </summary>
        public Status IsRestartService()
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            ServiceManager manager = new ServiceManager(EnvType.IIS);
            response.info = manager.IsRestartCoreHander(out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        #endregion

        #region 控制接口
        public Status WriteJZValue(int userID, int jzID, string fDBAddress, double value)
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            DeviceControl control = new DeviceControl();
            response.info = control.WriteJZValue(userID, jzID, fDBAddress, value,out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }
        public Status WriteSensorValue(int userID, string sensorID, double value)
        {
            Status response = new Status();
            string statusCode = ""; 
            string errMsg = "";
            DeviceControl control = new DeviceControl();
            response.info = control.WriteSensorValue(userID, sensorID, value, out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        #endregion

        #region 重载数据接口

        // 被废弃使用
        public Status ReLoadJZData()
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            DeviceControl control = new DeviceControl();
            response.info = control.ReLoadJZData(out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }
        public Status ReLoadJZData_RN() 
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            DeviceControl control = new DeviceControl();
            response.info = control.ReLoadJZData(out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        public Status ReLoadDataForJZ(int jzID)
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            DeviceControl control = new DeviceControl();
            response.info = control.ReLoadJZData(jzID, out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }
        public Status ReLoadDataForStation(int stationID)
        {
            Status response = new Status();
            string statusCode = "";
            string errMsg = "";
            DeviceControl control = new DeviceControl();
            response.info = control.ReLoadStationData(stationID, out statusCode, out errMsg);
            response.statusCode = statusCode;
            response.errMsg = errMsg;
            return response;
        }

        #endregion
    }
}
