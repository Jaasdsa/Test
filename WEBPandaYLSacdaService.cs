using CityIoTCommand;
using CityLogService;
using CityPublicClassLib;
using System;
using System.Collections.Concurrent;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace CityWEBDataService 
{
   public class WEBPandaYLSacdaService : ISonService, IServiceWorker
    {
        // WEB-压力监测点-SCADA 数据采集任务
        private System.Timers.Timer timer;
        private PandaParam param;
        private CommandConsumer commandCustomer;

        public void ReceiveCommand(RequestCommand command)
        {
            // 已经在入口验证过命令对象
            if (!IsRuning || this.commandCustomer == null || !this.commandCustomer.IsRuning)
            {
                CommandManager.MakeFail("Scada-WEB-压力监测点命令消费器运行异常", ref command);
                CommandManager.CompleteCommand(command);
                TraceManagerForCommand.AppendErrMsg("Scada-WEB-压力监测点命令消费器运行异常");
                return;
            }
            this.commandCustomer.Append(command);
        }

        public WEBPandaYLSacdaService(string configFilePath)
        {
            Config.configFilePath = configFilePath;
        }

        public void Start(out string errMsg)
        {
            errMsg = "";
            if (IsRuning)
                return;

            // 环境检查
            if (!EnvChecker.CheckPandaYaLiWEB(out errMsg))
                return;
            TraceManagerForWeb.AppendDebug("Scada-WEB-压力监测点环境检查通过");
            this.param = Config.pandaYaLiParam;

            WebPandaYLScadaCommand.CreateInitSensorRealData(param).Execute(); //初始化实时表

            timer = new System.Timers.Timer();
            timer.Interval = this.param.collectInterval * 60 * 1000;
            timer.Elapsed += (o, e) =>
            {
                try
                {
                    Excute();
                }
                catch(Exception ee)
                {
                    TraceManagerForWeb.AppendErrMsg("Scada-WEB-压力监测点定时任务执行失败:" + ee.Message);
                }
            };
            timer.Enabled = true;

            // 控制器服务
            if (commandCustomer != null)
                commandCustomer.Stop();
            commandCustomer = new CommandConsumer(ConsumerCommand);
            commandCustomer.Start();
            if (commandCustomer.IsRuning)
                TraceManagerForWeb.AppendDebug("Scada-WEB-压力监测点控制器服务已经打开");
            else
            {
                TraceManagerForWeb.AppendErrMsg("Scada-WEB-压力监测点控制器服务打开失败");
                Stop();
                return;
            }

            IsRuning = true;

            // 开始异步执行一次-防止启动卡死
            Action action = Excute;
            action.BeginInvoke(null, null);
        }
        public  bool IsRuning { get; set; }
        private bool ExcuteDoing { get; set; } = false;
        public  void Stop()
        {
            if (!IsRuning)
                return;

            try
            {
                // 控制器服务
                if (commandCustomer != null)
                {
                    commandCustomer.Stop();
                    if (!commandCustomer.IsRuning)
                    {
                        TraceManagerForWeb.AppendDebug("Scada-WEB-压力监测点控制器服务已停止");
                        this.commandCustomer = null;
                    }
                    else
                        TraceManagerForWeb.AppendErrMsg("Scada-WEB-压力监测点控制器服务停止失败");
                }
            }
            catch { }

            // 关闭定时器
            if (timer != null)
            {
                timer.Enabled = false;
                timer.Close();
                timer = null;
            }

            IsRuning = false;
        }

        private void Excute()
        {
            lock (this)
            {
                if (ExcuteDoing)
                    return;
                ExcuteDoing = true;
                ExcuteHandle();
                ExcuteDoing = false;
            }
        }
        private void ExcuteHandle()
        {
            // 报警维护
            WebPandaYLScadaCommand.CreateCollectAndSaveScadaSensors(param).Execute();
        }

        // 执行调度命令
        private void ConsumerCommand(RequestCommand command)
        {
            try
            {
                ExcuteCommand(command);
            }
            catch (Exception e)
            {
                CommandManager.MakeFail("Scada-WEB-压力监测点 定时任务执行失败:" + e.Message, ref command);
                CommandManager.CompleteCommand(command);
                TraceManagerForCommand.AppendErrMsg(command.message);
            }
        }
        private void ExcuteCommand(RequestCommand command)
        {
            if (command.sonServerType == CommandServerType.YL_WEB && command.operType == CommandOperType.ReLoadData)
            {
                if (ExcuteDoing) // 正在采集，等这次采集结束，在采集一次
                {
                    DateTime time1 = DateTime.Now;
                    while (true)
                    {
                        Thread.Sleep(1);
                        if (DateTime.Now - time1 > TimeSpan.FromSeconds(command.timeoutSeconds)) // 超时
                        {
                            CommandManager.MakeTimeout("Scada-WEB-压力监测点 数据更新超时", ref command);
                            CommandManager.CompleteCommand(command);
                            TraceManagerForCommand.AppendInfo(command.message);
                            return;
                        }
                        if (!ExcuteDoing)
                            break;
                    }
                }
                // 调取之前先重新加载一次缓存
                Excute();
                CommandManager.MakeSuccess("Scada-WEB-压力监测点 数据已更新", ref command);
                CommandManager.CompleteCommand(command);
                TraceManagerForCommand.AppendInfo("Scada-WEB-压力监测点 数据已更新");
                return;
            }
            CommandManager.MakeFail("错误的请求服务类型", ref command);
            CommandManager.CompleteCommand(command);
            TraceManagerForCommand.AppendErrMsg(command.message);
            return;
        }
    }
}
