using LeanCloud;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace WPFParseStarterProject
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += this.Application_Startup;

            // Initialize the Parse client with your Application ID and .NET Key found on
            // your Parse dashboard
            AVClient.Initialize("3knLr8wGGKUBiXpVAwDnryNT-gzGzoHsz", "3RpBhjoPXJjVWvPnVmPyFExt");
        }

        private async void Application_Startup(object sender, StartupEventArgs args)
        {
            //await AVAnalytics.TrackAppOpenedAsync();
            var avObject = new AVObject("TestObject");
            avObject["key"] = "value";
            await avObject.SaveAsync();

            Console.WriteLine(avObject.ObjectId);
        }
    }
}
