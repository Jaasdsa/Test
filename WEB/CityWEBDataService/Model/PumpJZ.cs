using CityPublicClassLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CityWEBDataService
{
    public class PumpJZ
    {
        public string _ID;
        public string _PumpName;
        public string _PumpJZName;
        public string _PandaPumpJZID;
        public int pointsVersionID;
        public Point[] points;
        public bool IsOnline { get; set; }

        public PumpJZValueBase ToPumpJZValueBase()
        {
            return new PumpJZValueBase()
            {
                _ID = this._ID,
                pointsVersionID = this.pointsVersionID,
                points = this.points,
                IsOnline = this.IsOnline
            };
        }
    }

    public class PandaPumpJZ
    {
        public string ID { get; set; }
        public string TempTime { get; set; }
        public int? FOnLine { get; set; }
        // 设备控制方式
        public string F41003 { get; set; }
        // 报警点
        public string F41004 { get; set; }
        public string F41005 { get; set; }

        public decimal? F41006 { get; set; }
        public decimal? F41007 { get; set; }
        // 泵状态--0、无泵，1、停止，2、运行，3、故障，4、工频
        public string F41008 { get; set; }
        public string F41009 { get; set; }
        public string F41010 { get; set; }
        public string F41011 { get; set; }
        public string F41012 { get; set; }
        public string F41013{ get; set; }
        // 泵频率
        public decimal? F41014{ get; set; }
        public decimal? F41015{ get; set; }
        public decimal? F41016{ get; set; }
        public decimal? F41017{ get; set; }
        public decimal? F41018{ get; set; }
        public decimal? F41019{ get; set; }
        // 水箱液位
        public decimal? F41020{ get; set; }
        public decimal? F41021{ get; set; }
        public decimal? F41022{ get; set; }
        public decimal? F41023{ get; set; }
        //流量
        public decimal? F41024{ get; set; }
        public decimal? F41025{ get; set; }
        // 变频器电流
        public decimal? F41045{ get; set; }
        public decimal? F41046{ get; set; }
        public decimal? F41047{ get; set; }
        public decimal? F41048{ get; set; }
        public decimal? F41049{ get; set; }
        public decimal? F41050{ get; set; }
        // 泵运行时间
        public decimal? F41051{ get; set; }
        public decimal? F41052{ get; set; }
        public decimal? F41053{ get; set; }
        public decimal? F41054{ get; set; }
        public decimal? F41055{ get; set; }
        public decimal? F41056{ get; set; }
        // 变频器功率
        public decimal? F41063{ get; set; }
        public decimal? F41064{ get; set; }
        public decimal? F41065{ get; set; }
        public decimal? F41066{ get; set; }
        public decimal? F41067{ get; set; }
        public decimal? F41068{ get; set; }
        // 水质
        public decimal? F41087{ get; set; }
        public decimal? F41088{ get; set; }
        public decimal? F41089{ get; set; }
        // 温湿度
        public decimal? F41090{ get; set; }
        public decimal? F41100{ get; set; }
        public decimal? F41101{ get; set; }
        //排水泵状态--0、无泵，1、停止，2、运行，3、故障
        public decimal? F41102{ get; set; }
        public decimal? F41103{ get; set; }
        public decimal? F41104{ get; set; }
        public decimal? F41105{ get; set; }
        // 累计电量、流量
        public int? FTotalDL{ get; set; }
        public int? FTotalOutLL{ get; set; }
    }
}
