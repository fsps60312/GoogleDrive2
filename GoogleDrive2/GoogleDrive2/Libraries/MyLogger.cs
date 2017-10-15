using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace GoogleDrive2
{
    public class MyLoggerClass
    {
        public delegate void LogAppendedEventHandler(string log);
        public LogAppendedEventHandler LogAppended,StatusUpdated;
        public void Log(string log) { LogAppended?.Invoke(log); }
        public void UpdateStatus(string status) { StatusUpdated?.Invoke(status); }
        public void RunLogger(MyLoggerClass logger,Action action,string name=null)
        {
            var logAppendedEventHandler = new LogAppendedEventHandler((log) => { this.Log($"{(name == null ? "" : $"[{name}]")}{log}"); });
            var statusUpdatedEventHandler = new LogAppendedEventHandler((status) => { this.UpdateStatus($"{(name == null ? "" : $"[{name}]")}{status}"); });
            logger.LogAppended += logAppendedEventHandler;
            logger.StatusUpdated += statusUpdatedEventHandler;
            try
            {
                action.Invoke();
            }
            finally
            {
                logger.LogAppended -= logAppendedEventHandler;
                logger.StatusUpdated -= statusUpdatedEventHandler;
            }
        }
        public async Task RunLogger(MyLoggerClass logger, Func<Task> action, string name = null)
        {
            var logAppendedEventHandler = new LogAppendedEventHandler((log) => { this.Log($"{(name == null ? "" : $"[{name}]")}{log}"); });
            var statusUpdatedEventHandler = new LogAppendedEventHandler((status) => { this.UpdateStatus($"{(name == null ? "" : $"[{name}]")}{status}"); });
            logger.LogAppended += logAppendedEventHandler;
            logger.StatusUpdated += statusUpdatedEventHandler;
            try
            {
                await action.Invoke();
            }
            finally
            {
                logger.LogAppended -= logAppendedEventHandler;
                logger.StatusUpdated -= statusUpdatedEventHandler;
            }
        }
        public void LogError(string log, bool printStackTrace = true)
        {
            MyLogger.LogError(log, printStackTrace);
            this.Log(MyLogger.CreateLog(log,printStackTrace));
        }
        public void Debug(string log,bool printStackTrace=false)
        {
            MyLogger.Debug(log, printStackTrace);
            this.Log(MyLogger.CreateLog(log, printStackTrace));
        }
    }
    partial class MyLogger
    {
        public delegate void LogAppendedEventHandler(string log);
        public static LogAppendedEventHandler ErrorLogged;
        public static Libraries.Events.MyEventHandler<string> Debugged;
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
