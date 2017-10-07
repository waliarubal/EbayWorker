using EbayWorker.Helpers;
using System;
using System.Diagnostics;
using System.Windows;

namespace EbayWorker
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (Debugger.IsAttached)
                return;

            var date = DateTime.Now.GetInternetTime();
            if (date.Date > new DateTime(2017, 10, 22).Date)
                Current.Shutdown(0);
        }
    }
}
