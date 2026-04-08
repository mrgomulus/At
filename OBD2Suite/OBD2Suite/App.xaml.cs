using System.Windows;
using OBD2Suite.Services;
using OBD2Suite.ViewModels;

namespace OBD2Suite
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            var obdService = new ObdService();
            var mainVm = new MainViewModel(obdService);
            var mainWindow = new MainWindow { DataContext = mainVm };
            mainWindow.Show();
        }
    }
}
