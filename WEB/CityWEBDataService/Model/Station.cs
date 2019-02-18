using CityPublicClassLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CityWEBDataService
{
    class Station
    {
        public int _ID;
        public string _GUID;
        public string _Name;
        public bool IsOnline { get; set; }
        public List<Sensor> sensors;

        public StaionValueBase ToStaionValueBase()
        {
           List<SnesorValueBase> valueBases=new List<SnesorValueBase>();
            if (sensors == null)
                valueBases = null;
            else
            {
                foreach (Sensor sensor in sensors)
                {
                    valueBases.Add(sensor.ToSnesorValueBase());
                }
            }
            return new StaionValueBase()
            {
                _ID = this._ID,
                IsOnline=this.IsOnline,
                sensors = valueBases.ToArray()
            };
        }
    }
    class Sensor:Point
    {
        public string sensorID;
        public string sensorName; 
        public int _PointAddressID;

        public SnesorValueBase ToSnesorValueBase()
        {
            return new SnesorValueBase()
            {
                _ID = this.sensorID,
                Value = this.Value,
                State = this.State,
                Mess = this.Mess,
                LastTime = this.LastTime,
            };
        }
    }

    class YaLiDian 
    {
        public string ID { get; set; }
        public string FDTUCode { get; set; }
        public string TempTime { get; set; }
        public int FOnLine { get; set; }
        public double? F40001 { get; set; }
        public double? F40017 { get; set; }
    }

    class ZongHeCeDian
    {
        public string ID { get; set; }
        public string FDTUCode { get; set; }
        public string TempTime { get; set; }
        public int FOnLine { get; set; }
        public double? F40001 { get; set; }
        public double? F40005 { get; set; }
        public double? F40011 { get; set; }
        public double? F40014 { get; set; }
        public double? F40015 { get; set; }
        public double? F40016 { get; set; }
        public double? F40038 { get; set; } 
    }
}
