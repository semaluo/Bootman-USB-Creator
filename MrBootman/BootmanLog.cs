using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Config;

namespace MrBootman
{
    #region Logging Functions

    public class BootmanLogData
    {
        public System.Windows.Controls.TextBox LogTextBox { get; set; }
        public string LogText { get; set; }
    }
  
    public static class BootmanLog
    {
        public static bool LogToDisplay { get; set; }

        // Declare Logging
        private static readonly ILog moLogger =
            LogManager.GetLogger(typeof(BootmanLog));

        public static void LogInfo(string in_sMessage)
        {
            moLogger.Info(in_sMessage);
        }

        public static void LogInfo(System.Windows.Controls.TextBox inout_oTextBox, string in_sMessage)
        {
            moLogger.Info(in_sMessage);
            if (LogToDisplay)
            {
                BootmanLogData oLogData = new BootmanLogData();
                oLogData.LogTextBox = inout_oTextBox;
                oLogData.LogText = in_sMessage;
                AppendToTextbox(oLogData);
            }
        }

        public static void LogError(string in_sMessage)
        {
            moLogger.Error(in_sMessage);
        }

        public static void LogError(System.Windows.Controls.TextBox inout_oTextBox, string in_sMessage)
        {
            moLogger.Error(in_sMessage);
            if (LogToDisplay)
            {
                BootmanLogData oLogData = new BootmanLogData();
                oLogData.LogTextBox = inout_oTextBox;
                oLogData.LogText = in_sMessage;
                AppendToTextbox(oLogData);
            }
        }

        public static void LogFatal(string in_sMessage)
        {
            moLogger.Fatal(in_sMessage);
        }

        public static void LogFatal(System.Windows.Controls.TextBox inout_oTextBox, string in_sMessage)
        {
            moLogger.Fatal(in_sMessage);
            if (LogToDisplay)
            {
                BootmanLogData oLogData = new BootmanLogData();
                oLogData.LogTextBox = inout_oTextBox;
                oLogData.LogText = in_sMessage;
                AppendToTextbox(oLogData);
            }
        }

        private delegate void AppendToTextboxDelegate(BootmanLogData oLogData);

        public static void AppendToTextbox(BootmanLogData oLogData)
        {
            if (oLogData.LogTextBox.Dispatcher.CheckAccess())
            {
                oLogData.LogTextBox.AppendText(oLogData.LogText + "\r\n");
                oLogData.LogTextBox.ScrollToEnd();
            }
            else
            {
                oLogData.LogTextBox.Dispatcher.Invoke(
                    System.Windows.Threading.DispatcherPriority.Normal,
                    new AppendToTextboxDelegate(AppendToTextbox), oLogData);
            }
        }
    }
    #endregion Logging Functions
}
