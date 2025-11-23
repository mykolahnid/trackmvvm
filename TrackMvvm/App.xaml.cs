using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TrackMvvm.Services;
using TrackMvvm.Utilities;
using TrackMvvm.Views;

namespace TrackMvvm
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        static App()
        {
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Simply create and show main window - it will handle auth
            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
