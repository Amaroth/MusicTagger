using System.Windows;

namespace MusicTagger2.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var login = new MainWindow();
            login.Load();
            login.Show();
        }
    }
}
