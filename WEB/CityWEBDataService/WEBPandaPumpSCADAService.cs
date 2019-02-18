using CityIoTCommand;
using CityLogService;
using CityPublicClassLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CityWEBDataService
{
   public class WEBPandaPumpSCADAService : ISonService, IServiceWorker
    {
        // Scada-WEB-二供 数据采集任务
        private System.Timers.Timer timer;
        private PandaParam param;
        private CommandConsumer commandCustomer;

        public void ReceiveCommand(RequestCommand command)
        {
            // 已经在入口验证过命令对象
            if (!IsRuning || this.commandCustomer == null || !this.commandCustomer.IsRuning)
            {
                CommandManager.MakeFail("Scada-WEB-二供 命令消费器运行异常", ref command);
                CommandManager.CompleteCommand(command);
                TraceManagerForCommand.AppendErrMsg("Scada-WEB-二供命令消费器运行异常");
                return;
            }
            this.commandCustomer.Append(command);
        }

        public WEBPandaPumpSCADAService(string configFilePath)
        {
            Config.configFilePath = configFilePath;
        } 

        public void Start(out string errMsg)
        {
            errMsg = "";
            if (IsRuning)
                return;

            // 环境检查
            if (!EnvChecker.CheckPandaPumpScadaWEB(out errMsg))
                return;
            TraceManagerForWeb.AppendDebug("Scada-WEB-二供 环境检查通过");
            this.param = Config.pandaPumpScadaParam;

            WebPandaPumpScadaCommand.CreateInitSensorRealData(param).Execute(); //初始化实时表

            timer = new System.Timers.Timer();
            timer.Interval = this.param.collectInterval * 60 * 1000;
            timer.Elapsed += (o, e) =>
            {
                try
                {
                    Excute();
                }
                catch (Exception ee)
                {
                    TraceManagerForWeb.AppendErrMsg("Scada-WEB-二供 定时任务执行失败:" + ee.Message);
                }
            };
            timer.Enabled = true;

            // 控制器服务
            if (commandCustomer != null)
                commandCustomer.Stop();
            commandCustomer = new CommandConsumer(ConsumerCommand);
            commandCustomer.Start();
            if (commandCustomer.IsRuning)
                TraceManagerForWeb.AppendDebug("Scada-WEB-二供 控制器服务已经打开");
            else
            {
                TraceManagerForWeb.AppendErrMsg("Scada-WEB-二供 控制器服务打开失败");
                Stop();
                return;
            }

            IsRuning = true;

            // 开始异步执行一次-防止启动卡死
            Action action = Excute;
            action.BeginInvoke(null, null);
        }
        public bool IsRuning { get; set; }
        private bool ExcuteDoing { get; set; } = false;
        public void Stop()
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
                        TraceManagerForWeb.AppendDebug("Scada-WEB-二供 控制器服务已停止");
                        this.commandCustomer = null;
                    }
                    else
                        TraceManagerForWeb.AppendErrMsg("Scada-WEB-二供 控制器服务停止失败");
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
            // 定时采集任务
            WebPandaPumpScadaCommand.CreateCollectAndSaveScadaSensors(param).Execute();
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
                CommandManager.MakeFail("Scada-WEB-二供 定时任务执行失败:" + e.Message, ref command);
                CommandManager.CompleteCommand(command);
                TraceManagerForCommand.AppendErrMsg(command.message);
            }
        }
        private void ExcuteCommand(RequestCommand command)
        {
            CommandManager.MakeFail("暂时不支持控制服务", ref command);
            CommandManager.CompleteCommand(command);
            TraceManagerForCommand.AppendErrMsg(command.message);
            return;
        }
    }
}
