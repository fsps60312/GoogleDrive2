using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

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
        public void LogError(string log)
        {
            MyLogger.LogError(log);
            this.Log(log);
        }
    }
    class MyLogger
    {
        public delegate void LogAppendedEventHandler(string log);
        public static LogAppendedEventHandler ErrorLogged;
        public static async Task Alert(string msg)
        {
            await App.Current.MainPage.DisplayAlert("", msg, "OK");
        }
        public static void LogError(string log)
        {
            ErrorLogged?.Invoke(log);
        }
        public static void Assert(bool condition)
        {
            if (!condition) MyLogger.LogError("Assertion failed!");
            System.Diagnostics.Debug.Assert(condition);
        }
        static MyLogger()
        {
            ErrorLogged += (log) =>
              {
                  System.Diagnostics.Debug.WriteLine(log);
              };
        }
    }
}
