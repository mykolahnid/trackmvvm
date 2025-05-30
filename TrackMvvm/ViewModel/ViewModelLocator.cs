/*
  In App.xaml:
  <Application.Resources>
      <vm:ViewModelLocator xmlns:vm="clr-namespace:TrackMvvm.ViewModel"
                           x:Key="Locator" />
  </Application.Resources>
  
  In the View:
  DataContext="{Binding Source={StaticResource Locator}, Path=Main}"
*/

using System;
using System.ComponentModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using TrackMvvm.Model;

namespace TrackMvvm.ViewModel
{
    /// <summary>
    /// This class contains static references to all the view models in the
    /// application and provides an entry point for the bindings.
    /// </summary>
    public class ViewModelLocator
    {
        private static IServiceProvider _serviceProvider;

        static ViewModelLocator()
        {
            ConfigureServices();
        }

        private static void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Register data services
            if (IsInDesignMode)
            {
                services.AddSingleton<IDataService, Design.DesignDataService>();
            }
            else
            {
                services.AddSingleton<IDataService, DataService>();
            }

            // Register ViewModels
            services.AddTransient<MainViewModel>();

            // Add other ViewModels as needed:
            // services.AddTransient<AddTaskViewModel>();
            // services.AddTransient<HistoryViewModel>();

            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Gets the Main property.
        /// </summary>
        public MainViewModel Main => _serviceProvider.GetRequiredService<MainViewModel>();

        /// <summary>
        /// Gets a value indicating whether the application is in design mode.
        /// </summary>
        public static bool IsInDesignMode
        {
            get
            {
                return DesignerProperties.GetIsInDesignMode(new DependencyObject()) ||
                       LicenseManager.UsageMode == LicenseUsageMode.Designtime;
            }
        }

        /// <summary>
        /// Cleans up all the resources.
        /// </summary>
        public static void Cleanup()
        {
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}