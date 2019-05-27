using System.ServiceProcess;

namespace DemoService
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        private static void Main()
        {
            var servicesToRun = new ServiceBase[]
            {
                new DemoService()
            };
            ServiceBase.Run(servicesToRun);
        }
    }
}
