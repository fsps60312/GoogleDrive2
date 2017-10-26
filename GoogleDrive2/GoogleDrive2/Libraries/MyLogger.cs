using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GoogleDrive2
{
    public class MyLoggerClass
    {
        public event Libraries.Events.MyEventHandler<string> ErrorLogged,Debugged;
        public void RunLogger(MyLoggerClass logger,Action action,string name=null)
        {
            var errorLoggedEventHandler = new Libraries.Events.MyEventHandler<string>((log) => { this.ErrorLogged?.Invoke($"{(name == null ? "" : $"[{name}]")}{log}"); });
            var debuggedEventHandler = new Libraries.Events.MyEventHandler<string>((status) => { this.Debugged?.Invoke($"{(name == null ? "" : $"[{name}]")}{status}"); });
            logger.ErrorLogged += errorLoggedEventHandler;
            logger.Debugged += debuggedEventHandler;
            try
            {
                action.Invoke();
            }
            finally
            {
                logger.ErrorLogged -= errorLoggedEventHandler;
                logger.Debugged -= debuggedEventHandler;
            }
        }
        public async Task RunLogger(MyLoggerClass logger, Task action, string name = null)
        {
            var errorLoggedEventHandler = new Libraries.Events.MyEventHandler<string>((log) => { this.ErrorLogged?.Invoke($"{(name == null ? "" : $"[{name}]")}{log}"); });
            var debuggedEventHandler = new Libraries.Events.MyEventHandler<string>((status) => { this.Debugged?.Invoke($"{(name == null ? "" : $"[{name}]")}{status}"); });
            logger.ErrorLogged += errorLoggedEventHandler;
            logger.Debugged += debuggedEventHandler;
            try
            {
                await action.AsAsyncAction();
            }
            finally
            {
                logger.ErrorLogged -= errorLoggedEventHandler;
                logger.Debugged -= debuggedEventHandler;
            }
        }
        public void LogError(string log, bool printStackTrace = true)
        {
            MyLogger.LogError(log, printStackTrace);
            ErrorLogged?.Invoke(MyLogger.CreateLog(log, printStackTrace));
        }
        public void Debug(string log,bool printStackTrace=false)
        {
            MyLogger.Debug(log, printStackTrace);
            Debugged?.Invoke(MyLogger.CreateLog(log, printStackTrace));
        }
    }
    partial class MyLogger
    {
        public static event Libraries.Events.MyEventHandler<string> ErrorLogged,Debugged;
        //public static async Task Alert(string msg)
        //{
        //    await App.Current.MainPage.DisplayAlert("", msg, "OK");
        //}
        static string StackTrace()
        {
            var ans = System.Environment.StackTrace;
            ans = ans.Substring(ans.IndexOf(Environment.NewLine, ans.IndexOf(Environment.NewLine, ans.IndexOf(Environment.NewLine) + 1) + 1) + 2);
            return ans;
        }
        public static string CreateLog(string log,bool printStackTrace)
        {
            var msg = log;
            if (printStackTrace) msg += $"\r\nStack Trace: {StackTrace()}";
            return msg;
        }
        public static void Debug(string log,bool printStackTrace=false)
        {
            ErrorLogged?.Invoke(CreateLog(log,printStackTrace));
        }
        public static void LogError(string log, bool printStackTrace = true)
        {
            ErrorLogged?.Invoke(CreateLog(log,printStackTrace));
        }
        public static void Assert(bool condition)
        {
            if (!condition) MyLogger.LogError("Assertion failed!");
            System.Diagnostics.Debug.Assert(condition);
        }
        static MyLogger()
        {
            ErrorLogged += (log) => { System.Diagnostics.Debug.WriteLine(log); };
            Debugged += (log) => { System.Diagnostics.Debug.WriteLine(log); };
        }
    }
}
