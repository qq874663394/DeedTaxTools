using System;
using System.Windows.Forms;
using ToolsHelper;

namespace WindowsFormsApp1
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new frmMain());
                Application.ThreadException += Application_ThreadException; //UI线程异常
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException; //多线程异常
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("系统", ex);
            }
        }
        /// <summary>
        /// 多线程异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            //可以记录日志并转向错误bug窗口友好提示用户
            Exception ex = e.ExceptionObject as Exception;
            LogHelper.WriteLog("多线程异常", ex);
        }
        /// <summary>
        /// UI线程异常
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            //可以记录日志并转向错误bug窗口友好提示用户
            LogHelper.WriteLog("UI线程异常", e.Exception);
        }
    }
}
