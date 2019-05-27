using System;
using System.IO;
using System.ServiceProcess;
using System.Threading;

namespace DemoService
{
    public partial class DemoService : ServiceBase
    {
        private Thread thread;

        public DemoService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            System.Diagnostics.Debug.WriteLine("Demo-Service starts ...");

            ThreadStart job = new ThreadStart(this.WriteToLog);
            this.thread = new Thread(job);
            this.thread.Start();
        }

        protected override void OnStop()
        {
            System.Diagnostics.Debug.WriteLine("Demo-Service stops ...");

            this.thread.Abort();
        }

        protected void WriteToLog()
        {
            while (true)
            {
                var appDataDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                var logDir = Path.Combine(appDataDir, "TestInstallerLogs");

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                var logFile = Path.Combine(logDir, "serviceLog.txt");
                var fs = new FileStream(logFile, FileMode.Append);
                var sw = new StreamWriter(fs);
                sw.Write("Log entry at " + DateTime.Now + "\r\n");
                sw.Close();
                fs.Close();
                Thread.Sleep(5000);
            }
        }
    }
}
