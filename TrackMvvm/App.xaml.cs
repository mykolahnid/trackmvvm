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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Try to get Supabase service from DI
            var supabaseService = Current.TryFindResource("Locator") is ViewModel.ViewModelLocator locator
                ? GetSupabaseServiceFromLocator()
                : null;

            // If Supabase is configured, handle authentication
            if (supabaseService != null)
            {
                await AuthenticateAsync(supabaseService);
            }
        }

        private static ISupabaseService? GetSupabaseServiceFromLocator()
        {
            try
            {
                // Access service provider via reflection to get ISupabaseService
                var locatorType = typeof(ViewModel.ViewModelLocator);
                var serviceProviderField = locatorType.GetField("_serviceProvider",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

                if (serviceProviderField?.GetValue(null) is IServiceProvider serviceProvider)
                {
                    return serviceProvider.GetService<ISupabaseService>();
                }
            }
            catch
            {
                // Supabase not configured
            }

            return null;
        }

        private static async Task AuthenticateAsync(ISupabaseService supabaseService)
        {
            // Check for stored credentials
            var storedCreds = CredentialStorage.RetrieveCredentials();

            if (storedCreds.HasValue)
            {
                // Try to authenticate with stored credentials
                var success = await supabaseService.AuthenticateAsync(
                    storedCreds.Value.Email,
                    storedCreds.Value.Password);

                if (success)
                    return; // Authentication successful
            }

            // No stored credentials or authentication failed - show login dialog
            var loginDialog = new LoginDialog();
            var result = loginDialog.ShowDialog();

            if (result == true)
            {
                var viewModel = loginDialog.ViewModel;
                var authSuccess = await supabaseService.AuthenticateAsync(
                    viewModel.Email,
                    viewModel.Password);

                if (authSuccess)
                {
                    // Store credentials if "Remember Me" is checked
                    if (viewModel.RememberMe)
                    {
                        CredentialStorage.StoreCredentials(viewModel.Email, viewModel.Password);
                    }
                }
                else
                {
                    MessageBox.Show(
                        "Authentication failed. Please check your credentials and try again.",
                        "Login Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);

                    Current.Shutdown();
                }
            }
            else
            {
                // User cancelled login
                Current.Shutdown();
            }
        }
    }
}
