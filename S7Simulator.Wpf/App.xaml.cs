using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace S7Simulator.Wpf
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Initialize log4net
            try
            {
                var logRepository = log4net.LogManager.GetRepository(System.Reflection.Assembly.GetEntryAssembly());
                
                // Try to find log4net.config in the application directory
                var configFile = new System.IO.FileInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log4net.config"));
                
                if (configFile.Exists)
                {
                    log4net.Config.XmlConfigurator.Configure(logRepository, configFile);
                    System.Diagnostics.Debug.WriteLine($"log4net configured from: {configFile.FullName}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"log4net.config not found at: {configFile.FullName}");
                    // Fallback: try to configure from embedded resource or default
                    log4net.Config.XmlConfigurator.Configure(logRepository);
                }
                
                // Test log to verify it's working
                var testLog = log4net.LogManager.GetLogger(typeof(App));
                testLog.Info("=== Application Started ===");
                System.Diagnostics.Debug.WriteLine("log4net initialization completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"log4net initialization failed: {ex.Message}");
            }
        }
    }
}
