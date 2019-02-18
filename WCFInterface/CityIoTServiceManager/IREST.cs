using CityServer.Lawer;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Text;

namespace CityIoTServiceManager
{
    [ServiceContract(Namespace = "http://www.mapgis.com.cn/civ")]
    public interface IREST
    {
        #region 服务接口测试

        /// <summary>
        /// 服务接口测试
        /// </summary>
        /// <param name = "test">接口参数测试</param>
        /// <returns>成功返回success，失败返回error</returns>
        [OperationContract]
        [Description("框架测试Get服务")]
        [WebInvoke(UriTemplate = "CaseManage/Test?test={test}", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status TestGet(string test);

        [OperationContract]
        [Description("框架测试Post服务")]
        [WebInvoke(UriTemplate = "CaseManage/TestPost", Method = "POST", ResponseFormat = WebMessageFormat.Json)]
        Person TestPost(Person person);

        #endregion

        #region 控制接口服务

        [OperationContract]
        [Description("机组写值接口")]
        [WebInvoke(UriTemplate = "CaseWrite/WriteJZValue?userID={userID}&jzID={jzID}&fDBAddress={fDBAddress}&value={value}", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status WriteJZValue(int userID, int jzID, string fDBAddress, double value);

        [OperationContract]
        [Description("站点写值接口")]
        [WebInvoke(UriTemplate = "CaseWrite/WriteSensorValue?userID={userID}&sensorID={sensorID}&value={value}", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status WriteSensorValue(int userID,string sensorID, double value);
        #endregion

        #region 重载数据接口

        [OperationContract]
        [Description("所有机组数据重新从设备获取一次---即将被废弃使用")]
        [WebInvoke(UriTemplate = "CaseWrite/ReLoadJZData", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status ReLoadJZData();

        [OperationContract]
        [Description("所有机组数据重新从设备获取一次--汝南临时专用---即将被废弃使用")]
        [WebInvoke(UriTemplate = "CaseReLoad/ReLoadJZData_RN", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status ReLoadJZData_RN();

        [OperationContract]
        [Description("该机组相同读取模式的机组都会被刷新")]
        [WebInvoke(UriTemplate = "CaseReLoad/ReLoadDataForJZ?jzID={jzID}", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status ReLoadDataForJZ(int jzID);

        [OperationContract]
        [Description("该站点相同读取模式的站点都会被刷新")]
        [WebInvoke(UriTemplate = "CaseReLoad/ReLoadDataForStation?stationID={stationID}", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status ReLoadDataForStation(int stationID);
        #endregion

        #region 服务管理

        /// <summary>
        /// 安装服务
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [Description("安装物联服务")]
        [WebInvoke(UriTemplate = "ServiceManager/IsInstalService", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status IsInstalService( );

        /// <summary>
        /// 卸载服务
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [Description("卸载物联服务")]
        [WebInvoke(UriTemplate = "ServiceManager/IsUnInstalService", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status IsUnInstalService();

        /// <summary>
        /// 启动服务
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [Description("启动物联服务")]
        [WebInvoke(UriTemplate = "ServiceManager/IsStartService", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status IsStartService();

        /// <summary>
        /// 停止服务
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [Description("停止物联服务")]
        [WebInvoke(UriTemplate = "ServiceManager/IsStopService", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status IsStopService();

        /// <summary>
        /// 重启服务
        /// </summary>
        /// <returns></returns>
        [OperationContract]
        [Description("重启物联服务")]
        [WebInvoke(UriTemplate = "ServiceManager/IsRestartService", Method = "GET", ResponseFormat = WebMessageFormat.Json)]
        Status IsRestartService(); 
        #endregion
    }
}
