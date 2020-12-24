using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace QotomReview.Views
{
    /// <summary>
    /// AboutWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow(string os_name, string os_ver)
        {
            InitializeComponent();
            this.WindowStyle = WindowStyle.ToolWindow;

            string temp1 = os.Text;
            string temp2 = os_version.Text;
            os.Text = temp1 + os_name;
            os_version.Text = temp2 + os_ver;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
