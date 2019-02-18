using CityLogService;
using CityPublicClassLib;
using CityUtils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CityWEBDataService
{
    public enum WebPandaZHCDScadaCommandType
    {
        CollectAndSaveScadaSensors,
        InitSensorRealData,
    }

    public class WebPandaZHCDScadaCommand
    {
        // SCADA-综合测点-web接入轮询执行实体类
        WebPandaZHCDScadaCommandType Type;
        PandaParam param;

        public static WebPandaZHCDScadaCommand CreateCollectAndSaveScadaSensors(PandaParam param)
        {
            return new WebPandaZHCDScadaCommand() { Type = WebPandaZHCDScadaCommandType.CollectAndSaveScadaSensors, param = param };
        }
        public static WebPandaZHCDScadaCommand CreateInitSensorRealData(PandaParam param)
        {
            return new WebPandaZHCDScadaCommand() { Type = WebPandaZHCDScadaCommandType.InitSensorRealData, param = param };
        }

        public static PandaToken tokenCache;
        private static DateTime lastSaveTime = DateTime.MinValue;

        // 执行体
        public void Execute()
        {
            try
            {
                lock (this)  //会被多线程调用注意安全
                {
                    switch (Type)
                    {
                        case WebPandaZHCDScadaCommandType.CollectAndSaveScadaSensors:
                            CollectAndSaveSnesors();
                            break;
                        case WebPandaZHCDScadaCommandType.InitSensorRealData:
                            InitSensorRealData();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                TraceManagerForWeb.AppendErrMsg("执行SCADA-综合测点-web接入数据库工作器失败：" + e.Message);
            }

        }
        private void CollectAndSaveSnesors()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start(); //  开始监视代码运行时间

            Dictionary<int, Station> dicStations = new Dictionary<int, Station>();
            #region 查询sensor表
            string sqlSensors = @"select * from( select t.ID stationID, t.GUID as stationCode,t.Name stationName,t1.ID sensorID,t1.Name sensorName,t1.PointAddressID 
                                                 from SCADA_Station t ,SCADA_Sensor t1 where t.ID=t1.StationID and t.ReadMode='WEB-ZHCD') x
                                                 left join PointAddressEntry x1 on x.PointAddressID=x1.ID;";
            DataTable dtSensors = DBUtil.ExecuteDataTable(sqlSensors, out string errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                TraceManagerForWeb.AppendErrMsg("查询sensor列表失败：" + errMsg);
                return;
            }
            foreach (DataRow dr in dtSensors.Rows)
            {
                Station station = new Station()
                {
                    _ID = DataUtil.ToInt(dr["stationID"]),
                    _GUID = DataUtil.ToString(dr["stationCode"]),
                    _Name = DataUtil.ToString(dr["stationName"]),
                    sensors = new List<Sensor>()
                };
                Sensor sensor = new Sensor()
                {
                    sensorID = DataUtil.ToString(dr["sensorID"]),
                    sensorName = DataUtil.ToString(dr["sensorName"]),
                    _PointAddressID = DataUtil.ToInt(dr["PointAddressID"]),
                    versionID = DataUtil.ToInt(dr["版本ID"]),
                    dataSourceAddress = DataUtil.ToString(dr["数据源地址"]).Trim(),
                    type = Point.ToType(DataUtil.ToString(dr["数据类型"])),
                    isActive = DataUtil.ToInt(dr["是否激活"]),
                    scale = DataUtil.ToDouble(dr["倍率"])
                };

                if (dicStations.Keys.Contains(station._ID))
                    dicStations[station._ID].sensors.Add(sensor);
                else
                {
                    station.sensors.Add(sensor);
                    dicStations.Add(station._ID, station);
                }
            }
            if (dicStations.Keys.Count == 0)
            {
                TraceManagerForWeb.AppendWarning("站点表没有读取模式:WEB-ZHCD的站点表");
                return;
            }
            #endregion

            #region 是否需求请求新token
            if (tokenCache == null || DateTime.Compare(DateTime.Now, DataUtil.ToDateTime(tokenCache.data.expireTime)) > 0)
                tokenCache = GetToken();
            if (tokenCache == null)
                return;
            if (DateTime.Compare(DateTime.Now, DataUtil.ToDateTime(tokenCache.data.expireTime)) > 0)
            {
                TraceManagerForWeb.AppendErrMsg("获取熊猫标记参数为过期标记");
                return;// 获取后还是过期秘钥
            }
            #endregion

            #region 请求监测点数据并存入
            List<ZongHeCeDian> ceDians = GetWebCeDianData();
            Collect(ceDians, ref dicStations);
            string saveSQL = GetSaveSensorsSQL(dicStations);
            if (string.IsNullOrWhiteSpace(saveSQL))
            {
                TraceManagerForWeb.AppendWarning(string.Format(@"采集WEB-综合测点数量{0}获取存入数据库SQL失败,可能原因没有在线的站点", dicStations.Keys.Count));
                return;
            }
            DBUtil.ExecuteNonQuery(saveSQL, out string err);
            stopwatch.Stop(); //  停止监视
            TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
            double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数

            if (!string.IsNullOrWhiteSpace(err))
                TraceManagerForWeb.AppendErrMsg("更新WEB-综合测点实时数据失败" + ",耗时:" + milliseconds.ToString() + "毫秒," + err);
            else
                TraceManagerForWeb.AppendDebug("更新WEB-综合测点实时数据成功" + ",耗时:" + milliseconds.ToString() + "毫秒");
            #endregion
        }

        // SCada执行体辅助工具
        private PandaToken GetToken()
        {
            PandaToken token = null;
            string AppKey = param.appKey;
            string appSecret = param.appSecret;
            string url = param.getTokenUrl;
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("AppKey", AppKey);
            parameters.Add("appSecret", appSecret);
            HttpRequestUtil requestUtil = new HttpRequestUtil(ContentType.XWwwFromUrlencoded);

            requestUtil.CreateSyncPostHttpRequest(url, parameters, (successData) =>
            {
                token = ByteUtil.ToDeserializeObject<PandaToken>(successData);
                if (token == null)
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫标记接口token失败:" + "返回的数据格式不正确");
                    return;
                }
                if (token.code == "200")
                {
                    TraceManagerForWeb.AppendDebug("获取熊猫标记接口token成功");
                    return;
                }
                else if (token.code == "10005")
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫标记接口token失败:" + "参数为空或格式不正确，确认appKey是否正确");
                    token = null;
                    return;
                }
                else if (token.code == "10006")
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫标记接口token失败:" + "Appkey和AppSecret不匹配");
                    token = null;
                    return;
                }
                else if (token.code == "49999")
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫标记接口token失败:" + "接口调用异常");
                    token = null;
                    return;
                }
                else
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫标记接口token失败:" + "错误的返回码");
                    token = null;
                    return;
                }

            }, (failData) => {
                token = null;
                TraceManagerForWeb.AppendErrMsg("获取熊猫标记接口token失败" + failData);
            }, (doErrorData) => {
                token = null;
                TraceManagerForWeb.AppendErrMsg("处理熊猫标记接口token数据失败" + doErrorData);
            });

            return token;
        }
        private List<ZongHeCeDian> GetWebCeDianData()
        {
            PandaWEBData pandaWEBData = new PandaWEBData();
            string url =param.getDataUrl;
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("accessToken", tokenCache.data.accessToken);
            parameters.Add("useName", param.useName);
            HttpRequestUtil requestUtil = new HttpRequestUtil(ContentType.XWwwFromUrlencoded);
            List<ZongHeCeDian> data = new List<ZongHeCeDian>();
            requestUtil.CreateSyncPostHttpRequest(url, parameters, (successData) =>
            {
                pandaWEBData = ByteUtil.ToDeserializeObject<PandaWEBData>(successData);
                if (pandaWEBData == null)
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫综合测点数据接口失败:" + "返回的数据格式不正确");
                    data = null;
                    return;
                }
                if (pandaWEBData.code == "200")
                {
                    TraceManagerForWeb.AppendDebug("获取熊猫综合点数据接口成功");
                    try
                    {
                        data = ByteUtil.ToDeserializeObject<List<ZongHeCeDian>>(pandaWEBData.data.ToString());
                    }
                    catch (Exception e)
                    {
                        data = null;
                        TraceManagerForWeb.AppendErrMsg("反序列化熊猫综合测点数据接口数据失败:" + e.Message);
                    }
                    return;
                }
                else
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫综合测点数据接口失败:" + pandaWEBData.msg);
                    data = null;
                    return;
                }

            }, (failData) => {
                data = null;
                TraceManagerForWeb.AppendErrMsg("获取熊猫综合测点数据接口失败" + failData);
            }, (doErrorData) => {
                data = null;
                TraceManagerForWeb.AppendErrMsg("处理熊猫综合测点数据接口失败" + doErrorData);
            });

            if (data == null)
                return null;
            return data;
        }
        private void Collect(List<ZongHeCeDian> ceDians, ref Dictionary<int, Station> dicStations)
        {
            if (ceDians == null)
                return;
            if (dicStations == null)
                return;            

            foreach (int key in dicStations.Keys)
            {
                Station station = dicStations[key];
                int errorTimes = 0; // 三个离线就认为其离线了
                foreach (Sensor sensor in dicStations[key].sensors)
                {
                    // 防止采集的点多了，错误消息炸了，每个都报出来了---直接让其离线
                    if (errorTimes >= 3)
                    {
                        TraceManagerForWeb.AppendErrMsg("StationName：" + station._Name + "三个条目采集失败,已跳过该站点采集，请检查点表和数据源");
                        dicStations[key].IsOnline = false;
                        break;
                    }

                    // 检查未通过
                    if (!sensor.CheckScadaWeb(out string err))
                    {
                        sensor.MakeFail(sensor.sensorName + err);
                        TraceManagerForWeb.AppendErrMsg("StationName：" + station._Name + "SensorName" + sensor.sensorName + " " + err);
                        errorTimes++;
                        continue;
                    }

                    // 拿到数据源
                    ZongHeCeDian[] curCeDians = ceDians.Where(y => y.ID.ToUpper() == station._GUID.ToUpper()).ToArray();// 注意转换大写在比较
                    if (curCeDians.Length == 0)
                    {
                        sensor.MakeFail("未在WEB综合测点数据源中找到配置站点信息,站点编码:" + station._GUID);
                        TraceManagerForWeb.AppendErrMsg("未在WEB综合测点数据源中找到配置站点信息,站点编码:" + station._GUID);
                        errorTimes++;
                        continue;
                    }
                    object pointDataSource;
                    string tempTime = DataUtil.ToDateString(DateTime.Now);
                    bool tempTimeFlag = false;
                    try
                    {
                        ZongHeCeDian curCeDian = curCeDians[0];
                        // 获取在线状态-防止sensor表没有配置在线状态
                        Type typeOnLine = curCeDian.GetType(); //获取类型
                        PropertyInfo propertyInfoOnLine = typeOnLine.GetProperty("FOnLine"); //获取采集时间的属性
                        object curOnLine = propertyInfoOnLine.GetValue(curCeDian, null);
                        if (curOnLine != null && DataUtil.ToInt(curOnLine) == 1)
                            dicStations[key].IsOnline = true;

                        // 先拿到时间
                        Type typeTempTime = curCeDian.GetType(); //获取类型
                        PropertyInfo propertyInfoTempTime = typeTempTime.GetProperty("TempTime"); //获取采集时间的属性
                        object curTempTime = propertyInfoTempTime.GetValue(curCeDian, null);
                        if (curTempTime != null && !string.IsNullOrWhiteSpace(curTempTime.ToString()))
                        {
                            tempTime = DataUtil.ToDateString(curTempTime); //获取采集时间属性值
                            tempTimeFlag = true;
                        }

                        // 在拿到值
                        Type type = curCeDian.GetType(); //获取类型
                        PropertyInfo propertyInfo = type.GetProperty(sensor.dataSourceAddress); //获取指定名称的属性
                        pointDataSource = propertyInfo.GetValue(curCeDian, null); //获取属性值
                    }
                    catch(Exception e)
                    {
                        string er = string.Format("未在WEB综合测点数据源中找到配置站点信息:{0}找到点地址为:{1}的点,错误原因:{2}" + station._Name, sensor.sensorName, e.Message);
                        sensor.MakeFail(er);
                        TraceManagerForWeb.AppendErrMsg(er);
                        errorTimes++;
                        continue;
                    }

                    // 根据数据源获取数据
                    sensor.ReadWEBPoint(pointDataSource);
                    sensor.LastTime = tempTime;// 使用采集时间，不要用当前时间

                    if (sensor.State == ValueState.Fail)
                    {
                        string er = string.Format("站点名称:{0},sensorName:{1},取值错误:{2}", station._Name, sensor.sensorName, sensor.Mess);
                        TraceManagerForWeb.AppendErrMsg(er);
                        errorTimes++;
                        continue;
                    }

                    // 判断采集时间是否正常
                    if (!tempTimeFlag)
                        dicStations[key].IsOnline = false;
                }
            }
        }
        private ValueBase ReadPoint(object pointDataSource, Point point)
        {
            ValueBase value = new ValueBase();
            value.State = ValueState.Success;
            value.LastTime = DataUtil.ToDateString(DateTime.Now);

            switch (point.type)
            {
                case PointType.String:
                    value.Value = pointDataSource;
                    break;
                case PointType.Ushort:
                    value.Value = DataUtil.ToDouble(pointDataSource);
                    break;
                case PointType.Real:
                    value.Value = DataUtil.ToDouble(pointDataSource) * point.scale;
                    break;
                default:
                    value.MakeFail("点ID:" + point.pointID + "未知的数据类型");
                    break;
            }
            return value;
        }
        private string GetSaveSensorsSQL(Dictionary<int, Station> dicStations)
        {
            if (dicStations == null || dicStations.Keys.Count == 0)
                return "";
            List<StaionValueBase> stationBases = new List<StaionValueBase>();
            foreach (int stationID in dicStations.Keys)
            {
                stationBases.Add(dicStations[stationID].ToStaionValueBase());
            }
            string sensorsRealSQL = "";
            string sensorsHisSQL = "";
            StaionDataOper.Instance.GetMulitStationRealSQL(stationBases.ToArray(), out sensorsRealSQL);
            if (DateTime.Now - lastSaveTime > TimeSpan.FromSeconds(param.saveInterVal * 60))
            {
                StaionDataOper.Instance.GetMulitStationHisSQL(stationBases.ToArray(), out sensorsHisSQL);
                lastSaveTime = DateTime.Now;
            }
            return sensorsRealSQL + sensorsHisSQL;
        }
        // 实时表是否在线字段实时维护
        private void InitSensorRealData()
        {
            StaionDataOper.Instance.InitSCADASensorRealTime(out string errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
                TraceManagerForWeb.AppendErrMsg(errMsg);
        }
    }
}
