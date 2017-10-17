using System.ComponentModel;
using System.Windows;

namespace EbayWorker.Views
{
    /// <summary>
    /// Interaction logic for HomeView.xaml
    /// </summary>
    public partial class HomeView
    {
        public HomeView()
        {
            InitializeComponent();
        }

        void Window_Closing(object sender, CancelEventArgs e)
        { 
            if (MessageBox.Show(this, "Are you sure you want to close?", "Close", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.No)
            {
                e.Cancel = true;
                return;
            }

            Application.Current.Shutdown(0);
        }
    }
}
