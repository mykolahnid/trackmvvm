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
using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrackMvvm.Model;
using TrackMvvm.Services;

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

            // Load configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: false)
                .Build();

            // Register data services
            if (IsInDesignMode)
            {
                services.AddSingleton<IDataService, Design.DesignDataService>();
            }
            else
            {
                services.AddSingleton<IDataService, DataService>();

                // Register Supabase service if configuration exists
                var supabaseUrl = configuration["Supabase:Url"];
                var supabaseKey = configuration["Supabase:AnonKey"];

                if (!string.IsNullOrEmpty(supabaseUrl) && !string.IsNullOrEmpty(supabaseKey))
                {
                    services.AddSingleton<ISupabaseService>(sp =>
                        new SupabaseService(supabaseUrl, supabaseKey));

                    // Register Sync service (depends on Supabase service)
                    services.AddSingleton<ISyncService>(sp =>
                        new SyncService(sp.GetRequiredService<ISupabaseService>()));
                }
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
        /// Gets the Supabase service if configured
        /// </summary>
        public static ISupabaseService? SupabaseService => _serviceProvider?.GetService<ISupabaseService>();

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