using CityLogService;
using CityPublicClassLib;
using CityUtils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CityWEBDataService
{

    enum WebPandaPumpCommandType
    {
        CollectAndSavePumpPoints,
        InitPumpRealData,
    }

    class WebPandaPumpCommand
    {
        // 二供web接入轮询执行实体类
        WebPandaPumpCommandType Type;
        PandaParam param;

        public static WebPandaPumpCommand CreateCollectAndSavePumpPoints(PandaParam param)
        {
            return new WebPandaPumpCommand() { Type = WebPandaPumpCommandType.CollectAndSavePumpPoints, param = param };
        }
        public static WebPandaPumpCommand CreateInitPumpRealData(PandaParam param)
        {
            return new WebPandaPumpCommand() { Type = WebPandaPumpCommandType.InitPumpRealData, param = param };
        }

        public static PandaToken tokenCache;
        private static Dictionary<string, string> _LastUpdateDays = new Dictionary<string, string>();
        private static DateTime lastSaveTime = DateTime.MinValue;
        private Dictionary<int, List<Point>> pointsCache;

        // 执行体
        public void Execute()
        {
            try
            {
                lock (this)  //会被多线程调用注意安全
                {
                    switch (Type)
                    {
                        case WebPandaPumpCommandType.CollectAndSavePumpPoints:
                            CollectAndSavePoints();
                            break;
                        case WebPandaPumpCommandType.InitPumpRealData:
                            InitPumpRealData();
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                TraceManagerForWeb.AppendErrMsg("执行数据库工作器失败：" + e.Message);
            }

        }
        private void CollectAndSavePoints()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start(); //  开始监视代码运行时间

            #region 查询点表
            string sqlPoints = @"select t.*,t1.数据业务地址,t1.名称 from PointAddressEntry t,PumpPointAddressDetail t1  where t.点明细ID=t1.ID 
                                     and t.版本ID in(select distinct PointAddressID  from PumpJZ x,pump x1 where PumpJZReadMode='WEB-PUMP' and (x.是否删除=0 or x.是否删除 is null) and (x1.是否删除=0 or x1.是否删除 is null));";
            DataTable dtPoints = DBUtil.ExecuteDataTable(sqlPoints, out string errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                TraceManagerForWeb.AppendErrMsg("查询二供点表版本失败：" + errMsg);
                return;
            }
            pointsCache = new Dictionary<int, List<Point>>();
            foreach (DataRow dr in dtPoints.Rows)
            {
                int versionID = DataUtil.ToInt(dr["版本ID"]);
                Point point = new Point()
                {
                    pointID = DataUtil.ToInt(dr["ID"]),
                    versionID = versionID,
                    name = DataUtil.ToString(dr["名称"]),
                    dataSourceAddress = DataUtil.ToString(dr["数据源地址"]).Trim(),
                    offsetAddress = DataUtil.ToString(dr["偏移地址"]).Trim(),
                    dbSAddress = DataUtil.ToString(dr["数据业务地址"]).Trim(),
                    type = Point.ToType(DataUtil.ToString(dr["数据类型"])),
                    isActive = DataUtil.ToInt(dr["是否激活"]),
                    isWrite = DataUtil.ToInt(dr["是否写入"]),
                    scale = DataUtil.ToDouble(dr["倍率"])
                };
                if (pointsCache.Keys.Contains(versionID))
                    pointsCache[versionID].Add(point);
                else
                    pointsCache.Add(versionID, new List<Point>() { point });
            }
            if (pointsCache.Keys.Count == 0)
            {
                TraceManagerForWeb.AppendWarning("机组表没有读取模式关于WEB-PUMP的点表");
                return;
            }
            #endregion

            #region 查询JZ表
            string sqlJZ = @"select CONVERT(varchar(50),t1.ID) as BASEID ,t.PName as PumpName,t1.PumpJZName , t1.PointAddressID,t1.JZCode  
                             from Pump t, PumpJZ t1 where t.ID=t1.PumpId and t1.PumpJZReadMode='WEB-PUMP' and (t.是否删除=0 or t.是否删除 is null)  and (t1.是否删除=0 or t1.是否删除 is null) ;";

            DataTable dtJZIDs = DBUtil.ExecuteDataTable(sqlJZ, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                TraceManagerForWeb.AppendErrMsg("查询二供机组ID列表失败：" + errMsg);
                return;
            }
            List<PumpJZ> jzs = new List<PumpJZ>();
            foreach (DataRow dr in dtJZIDs.Rows)
            {
                int versionID = DataUtil.ToInt(dr["PointAddressID"]);
                List<Point> points = pointsCache[versionID];
                Point[] pointsCopy = ByteUtil.DeepClone<List<Point>>(points).ToArray();// 一定要深度辅助一个副本，防止引用类型
                PumpJZ jz = new PumpJZ()
                {
                    _ID = DataUtil.ToString(dr["BASEID"]),
                    _PumpName = DataUtil.ToString(dr["PumpName"]),
                    _PumpJZName = DataUtil.ToString(dr["PumpJZName"]),
                    _PandaPumpJZID = DataUtil.ToString(dr["JZCode"]),
                    pointsVersionID = versionID,
                    points = pointsCopy
                };
                jzs.Add(jz);
                // 生成机组更新日期字典
                if (!_LastUpdateDays.Keys.Contains(jz._ID))
                    _LastUpdateDays.Add(jz._ID, "");
            }
            if (jzs.Count == 0)
            {
                TraceManagerForWeb.AppendWarning("机组表没有读取模式为WEB-PUMP的有效机组");
                return;
            }
            #endregion

            #region 查询天表
            string sqlHisDayDatas = @"select BASEID,TempTime from(
                                      select row_number() over(partition by BASEID order by TempTime desc) nRow ,* from PumpHisDayData tt) t where t.nRow=1";
            DataTable dtHisDayDatas = DBUtil.ExecuteDataTable(sqlHisDayDatas, out errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
            {
                TraceManagerForWeb.AppendErrMsg("查询二供历史天表缓存信息失败：" + errMsg);
                return;
            }
            String[] keyArr = _LastUpdateDays.Keys.ToArray<String>();
            for (int i = 0; i < keyArr.Length; i++)
            {
                DataRow[] drs = dtHisDayDatas.Select("BASEID='" + keyArr[i] + "'");
                if (drs != null && drs.Length > 0)
                    _LastUpdateDays[keyArr[i]] = DataUtil.ToDateString(drs[0]["TempTime"]);
            }
            #endregion

            #region 是否需求请求新token
            if (tokenCache == null || DateTime.Compare(DateTime.Now, DataUtil.ToDateTime(tokenCache.data.expireTime)) > 0)
                tokenCache = GetToken();
            if (tokenCache == null)
                return;
            if (DateTime.Compare(DateTime.Now, DataUtil.ToDateTime(tokenCache.data.expireTime)) > 0)
            {
                TraceManagerForWeb.AppendErrMsg("获取二供标记参数为过期标记");
                return;// 获取后还是过期秘钥
            }
            #endregion

            #region 请求二供数据并存入
            List<PandaPumpJZ> pandaJZs = GetWebPumpData();
            Collect(pandaJZs, ref jzs);
            string saveSQL = GetSavePointsSQL(jzs);
            if (string.IsNullOrWhiteSpace(saveSQL))
            {
                TraceManagerForWeb.AppendWarning(string.Format(@"采集机组数量{0}获取存入数据库SQL失败,可能原因没有在线的机组", jzs.Count));
                return;
            }
            DBUtil.ExecuteNonQuery(saveSQL, out string err);
            stopwatch.Stop(); //  停止监视
            TimeSpan timespan = stopwatch.Elapsed; //  获取当前实例测量得出的总时间
            double milliseconds = timespan.TotalMilliseconds;  //  总毫秒数

            if (!string.IsNullOrWhiteSpace(err))
                TraceManagerForWeb.AppendErrMsg("更新二供实时数据失败" + ",耗时:" + milliseconds.ToString() + "毫秒," + err);
            else
                TraceManagerForWeb.AppendDebug("更新二供实时数据成功" + ",耗时:" + milliseconds.ToString() + "毫秒");
            #endregion
        }

        // 二供执行体辅助工具
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
                    TraceManagerForWeb.AppendErrMsg("获取熊猫二供接口token失败:" + "返回的数据格式不正确");
                    return;
                }
                if (token.code == "200")
                {
                    TraceManagerForWeb.AppendDebug("获取熊猫二供接口token成功");
                    return;
                }
                else if (token.code == "10005")
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫二供接口token失败:" + "参数为空或格式不正确，确认appKey是否正确");
                    token = null;
                    return;
                }
                else if (token.code == "10006")
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫二供接口token失败:" + "Appkey和AppSecret不匹配");
                    token = null;
                    return;
                }
                else if (token.code == "49999")
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫二供接口token失败:" + "接口调用异常");
                    token = null;
                    return;
                }
                else
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫二供接口token失败:" + "错误的返回码");
                    token = null;
                    return;
                }

            }, (failData) => {
                token = null;
                TraceManagerForWeb.AppendErrMsg("获取熊猫二供接口token失败" + failData);
            }, (doErrorData) => {
                token = null;
                TraceManagerForWeb.AppendErrMsg("处理熊猫二供接口token数据失败" + doErrorData);
            });

            return token;
        }
        private List<PandaPumpJZ> GetWebPumpData()
        {
            PandaWEBData pandaPumpData = new PandaWEBData();
            string url = param.getDataUrl;
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("accessToken", tokenCache.data.accessToken);
            parameters.Add("useName", param.useName);
            HttpRequestUtil requestUtil = new HttpRequestUtil(ContentType.XWwwFromUrlencoded);
            List<PandaPumpJZ> data = new List<PandaPumpJZ>();
            requestUtil.CreateSyncPostHttpRequest(url, parameters, (successData) =>
            {
                pandaPumpData = ByteUtil.ToDeserializeObject<PandaWEBData>(successData);
                if (pandaPumpData == null)
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫二供数据接口失败:" + "返回的数据格式不正确");
                    data = null;
                    return;
                }
                if (pandaPumpData.code == "200")
                {
                    TraceManagerForWeb.AppendDebug("获取熊猫二供数据接口成功");
                    try
                    {
                        data = ByteUtil.ToDeserializeObject<List<PandaPumpJZ>>(pandaPumpData.data.ToString());
                    }
                    catch (Exception e)
                    {
                        data = null;
                        TraceManagerForWeb.AppendErrMsg("反序列化熊猫二供数据接口数据失败:" + e.Message);
                    }
                    return;
                }
                else
                {
                    TraceManagerForWeb.AppendErrMsg("获取熊猫二供数据接口失败:" + pandaPumpData.msg);
                    data = null;
                    return;
                }

            }, (failData) => {
                data = null;
                TraceManagerForWeb.AppendErrMsg("获取熊猫二供数据接口失败" + failData);
            }, (doErrorData) => {
                data = null;
                TraceManagerForWeb.AppendErrMsg("处理熊猫二供数据接口失败" + doErrorData);
            });

            if (data == null)
                return null;
            return data;
        }
        private void Collect(List<PandaPumpJZ> pandaJZs, ref List<PumpJZ> jzs)
        {
            if (pandaJZs == null)
                return;
            if (jzs == null)
                return;

            foreach (PumpJZ jz in jzs)
            {
                int errorTimes = 0; // 三个离线就认为该机组离线了
                foreach (Point point in jz.points)
                {
                    // 防止采集的点多了，错误消息炸了，每个都报出来了---直接让机组离线
                    if (errorTimes >= 3)
                    {
                        TraceManagerForWeb.AppendErrMsg(jz._PumpName + jz._PumpJZName + "三个条目采集失败,已跳过该机组采集，请检查点表和数据源");
                        jz.IsOnline = false;
                        break;
                    }

                    // 检查未通过
                    if (!point.CheckPumpWeb(out string err))
                    {
                        point.MakeFail(jz._PumpName + jz._PumpJZName + point.name + err);
                        TraceManagerForWeb.AppendErrMsg(jz._PumpName + jz._PumpJZName + point.name + err);
                        errorTimes++;
                        continue;
                    }

                    // 拿到数据源
                    PandaPumpJZ[] curPandaPumpJZs = pandaJZs.Where(p => p.ID.ToUpper() == jz._PandaPumpJZID.ToUpper()).ToArray();// 注意转换大写在比较
                    if (curPandaPumpJZs.Length == 0)
                    {
                        point.MakeFail("未在WEB数据源中找到机组实时信息,机组编码:" + jz._PandaPumpJZID);
                        TraceManagerForWeb.AppendErrMsg("未在WEB数据源中找到机组实时信息,机组编码:" + jz._PandaPumpJZID);
                        errorTimes++;
                        continue;
                    }
                    object pointDataSource;
                    string tempTime = DataUtil.ToDateString(DateTime.Now);
                    bool tempTimeFlag = false;
                    try
                    {
                        PandaPumpJZ curPandaPumpJZ = curPandaPumpJZs[0];

                        // 获取在线状态-防止点表没有配置在线状态
                        Type typeOnLine = curPandaPumpJZ.GetType(); //获取类型
                        PropertyInfo propertyInfoOnLine = typeOnLine.GetProperty("FOnLine"); //获取采集时间的属性
                        object curOnLine = propertyInfoOnLine.GetValue(curPandaPumpJZ, null);
                        if (curOnLine != null && DataUtil.ToInt(curOnLine) == 1)
                            jz.IsOnline = true;

                        // 先拿到时间
                        Type typeTempTime = curPandaPumpJZ.GetType(); //获取类型
                        PropertyInfo propertyInfoTempTime = typeTempTime.GetProperty("TempTime"); //获取采集时间的属性
                        object curTempTime = propertyInfoTempTime.GetValue(curPandaPumpJZ, null);
                        if (curTempTime != null && !string.IsNullOrWhiteSpace(curTempTime.ToString()))
                        {
                            tempTime = DataUtil.ToDateString(curTempTime); //获取采集时间属性值
                            tempTimeFlag = true;
                        }

                        // 在拿到值
                        Type type = curPandaPumpJZ.GetType(); //获取类型
                        System.Reflection.PropertyInfo propertyInfo = type.GetProperty(point.dataSourceAddress); //获取指定名称的属性
                        pointDataSource = propertyInfo.GetValue(curPandaPumpJZ, null); //获取属性值
                    }
                    catch (Exception e)
                    {
                        string er = string.Format("{0}-{1}-{2}采集失败,错误的原因:{3}", jz._PumpName, jz._PumpJZName, point.name, e.Message);
                        point.MakeFail(er);
                        TraceManagerForWeb.AppendErrMsg(er);
                        errorTimes++;
                        continue;
                    }
                    // 根据数据源获取数据
                    point.ReadWEBPoint(pointDataSource);
                    if (point.State == ValueState.Fail)
                    {
                        string er = string.Format("{0}-{1}-{2}采集失败,取值错误:{3}", jz._PumpName, jz._PumpJZName, point.name, point.Mess);
                        TraceManagerForWeb.AppendErrMsg(er);
                        errorTimes++;
                        continue;
                    }

                    // 判断采集时间是否正常
                    if (!tempTimeFlag)
                        jz.IsOnline = false;
                }
            }
        }
        private string GetSavePointsSQL(List<PumpJZ> jzs)
        {
            if (jzs == null || jzs.Count == 0)
                return "";
            List<PumpJZValueBase> jsBases = new List<PumpJZValueBase>();
            foreach (PumpJZ jz in jzs)
            {
                jsBases.Add(jz.ToPumpJZValueBase());
            }
            string pointsRealSQL = "";
            string pointsHisSQL = "";
            string pointsHisDaySQL = "";
            PumpJZDataOper.Instance.GetMulitJZRealSQL(jsBases.ToArray(), out pointsRealSQL);
            if (DateTime.Now - lastSaveTime > TimeSpan.FromSeconds(param.saveInterVal * 60))
            {
                PumpJZDataOper.Instance.GetMulitJZHisSQL(jsBases.ToArray(), out pointsHisSQL, ref _LastUpdateDays, out pointsHisDaySQL);
                lastSaveTime = DateTime.Now;
            }
            return pointsRealSQL + pointsHisSQL + pointsHisDaySQL;
        }

        // 实时表是否在线字段实时维护
        private void InitPumpRealData()
        {
            PumpJZDataOper.Instance.InitPumpRealData(out string errMsg);
            if (!string.IsNullOrWhiteSpace(errMsg))
                TraceManagerForWeb.AppendErrMsg(errMsg);
        }
    }
}
