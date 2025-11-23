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

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Get Supabase service from DI
                var supabaseService = ViewModel.ViewModelLocator.SupabaseService;

                // If Supabase is configured, handle authentication
                if (supabaseService != null)
                {
                    await AuthenticateAsync(supabaseService);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Startup error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Critical Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private static async Task AuthenticateAsync(ISupabaseService supabaseService)
        {
            try
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

                    MessageBox.Show($"Attempting to authenticate with:\nEmail: {viewModel.Email}\nPassword length: {viewModel.Password?.Length ?? 0}", "Debug");

                    var authSuccess = await supabaseService.AuthenticateAsync(
                        viewModel.Email,
                        viewModel.Password);

                    MessageBox.Show($"Authentication result: {authSuccess}", "Debug");

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
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Authentication error: {ex.Message}\n\nStack trace:\n{ex.StackTrace}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Current.Shutdown();
            }
        }
    }
}
