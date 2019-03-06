using System.Windows;

namespace MusicTagger.GUI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            var mainWindow = new MainWindow();
            mainWindow.Show();
            if (e.Args.Length > 0)
                mainWindow.OpenFile(e.Args[0]);
        }
    }
}
