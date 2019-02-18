using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CityWEBDataService
{
    public class PandaParam
    {
        public string appKey;
        public string appSecret;
        public string getTokenUrl;
        public string getDataUrl;
        public string useName;

        public int collectInterval;
        public int saveInterVal;

        public bool Check(out string errMsg)
        {
            errMsg = "";
            if (string.IsNullOrWhiteSpace(appKey))
            {
                errMsg = "appKey不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(appSecret))
            {
                errMsg = "appSecret不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(getTokenUrl))
            {
                errMsg = "getTokenUrl不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(getDataUrl))
            {
                errMsg = "getPumpUrl不能为空";
                return false;
            }
            if (string.IsNullOrWhiteSpace(useName))
            {
                errMsg = "useName不能为空";
                return false;
            }
            if (collectInterval == 0)
            {
                errMsg = "读取间隔时间不能为0";
                return false;
            }
            if (saveInterVal==0)
            {
                errMsg = "历史存入时间不能为0";
                return false;
            }
            return true;
        }
    }

    public class PandaToken
    {
        public PandaToken()
        {
            this.data = new PandaTokenData();
        }
        public PandaTokenData data;
        public string code;
        public string msg;
    }

    public class PandaWEBData
    {
        public object data;
        public string code;
        public string msg;
    }

    public class PandaTokenData
    {
        public string accessToken;
        public string expireTime;
    }
}
