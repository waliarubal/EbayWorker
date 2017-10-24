using EbayWorker.Helpers;
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
            Analytics.Instance.StartSession();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Analytics.Instance.EndSession();
            base.OnExit(e);
        }
    }
}
