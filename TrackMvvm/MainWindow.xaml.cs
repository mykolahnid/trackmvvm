using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrackMvvm.ViewModel;
using TrackMvvm.Views;

namespace TrackMvvm
{
    public class AddTaskDialogMessage : ValueChangedMessage<Action<string>>
    {
        public AddTaskDialogMessage(Action<string> callback) : base(callback) { }

        public Action<string> Callback => Value;
    }

    public class TaskStartedMessage
    {
        public string TaskName { get; set; }
        public TaskStartedMessage(string taskName) => TaskName = taskName;
    }

    public class ShowHistoryMessage
    {
        public WorkSessionHistoryViewModel HistoryViewModel { get; set; }
        public Action<bool> Callback { get; set; }
        public ShowHistoryMessage(WorkSessionHistoryViewModel historyViewModel, Action<bool> callback)
        {
            HistoryViewModel = historyViewModel;
            Callback = callback;
        }
    }

    public class AuthenticationCompletedMessage
    {
        public bool Success { get; set; }
        public AuthenticationCompletedMessage(bool success) => Success = success;
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            WeakReferenceMessenger.Default.Register<AddTaskDialogMessage, string>(this, "MainWindow", (r, m) =>
            {
                System.Diagnostics.Debug.WriteLine($"Message received!");
                AskForTaskName(m);
            });
            System.Diagnostics.Debug.WriteLine("MainWindow registered for messages");
            WeakReferenceMessenger.Default.Register<TaskStartedMessage>(this, (r, m) => OnTaskStarted(m));
            WeakReferenceMessenger.Default.Register<ShowHistoryMessage>(this, (r, m) => OnShowHistory(m));

            Closing += (s, e) => ViewModelLocator.Cleanup();
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get Supabase service from DI
            var supabaseService = ViewModelLocator.SupabaseService;

            System.Diagnostics.Debug.WriteLine($"[MainWindow] Got SupabaseService from DI. HashCode: {supabaseService?.GetHashCode()}");

            if (supabaseService != null)
            {
                bool authSuccess = await HandleAuthenticationAsync(supabaseService);

                System.Diagnostics.Debug.WriteLine($"[MainWindow] Authentication completed. Success: {authSuccess}");

                // Notify ViewModels about authentication result
                WeakReferenceMessenger.Default.Send(new AuthenticationCompletedMessage(authSuccess));

                if (!authSuccess)
                {
                    // Authentication failed or cancelled - close app
                    Application.Current.Shutdown();
                }
            }
        }

        private async System.Threading.Tasks.Task<bool> HandleAuthenticationAsync(Services.ISupabaseService supabaseService)
        {
            // Check for stored credentials
            var storedCreds = Utilities.CredentialStorage.RetrieveCredentials();

            if (storedCreds.HasValue)
            {
                // Try to authenticate with stored credentials
                var success = await supabaseService.AuthenticateAsync(
                    storedCreds.Value.Email,
                    storedCreds.Value.Password);

                if (success)
                    return true; // Authentication successful
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
                        Utilities.CredentialStorage.StoreCredentials(viewModel.Email, viewModel.Password);
                    }
                    return true;
                }
                else
                {
                    MessageBox.Show(
                        "Authentication failed. Please check your credentials and try again.",
                        "Login Error",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }
            }

            // User cancelled
            return false;
        }

        private static void OnShowHistory(ShowHistoryMessage message)
        {
            //hack, target should not be used this way
            var dialog = new HistoryDialog {DataContext = message.HistoryViewModel};

            var messageBoxResult = dialog.ShowDialog();

            if (messageBoxResult == true)
            {
                message.Callback(true);
            }
        }

        private void OnTaskStarted(TaskStartedMessage message)
        {
            var text = message.TaskName;

            if (text == string.Empty)
            {
                TaskbarItemInfo.Overlay = null;
                return;
            }

            int iconWidth = 20;
            int iconHeight = 20;

            string countText = text.Trim();

            RenderTargetBitmap bmp =
                new RenderTargetBitmap(iconWidth, iconHeight, 96, 96, PixelFormats.Default);

            ContentControl root = new ContentControl();

            root.ContentTemplate = ((DataTemplate)Resources["OverlayIcon"]);
            root.Content = countText;

            root.Arrange(new Rect(0, 0, iconWidth, iconHeight));

            bmp.Render(root);

            TaskbarItemInfo.Overlay = bmp;
        }

        private static void AskForTaskName(AddTaskDialogMessage message)
        {
            var dialog = new AddTaskDialog { DataContext = new AddTaskViewModel() };

            if (dialog.ShowDialog() == true)
            {
                var viewModel = dialog.DataContext as AddTaskViewModel;
                message.Callback?.Invoke(viewModel?.TaskName);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HistoryDialog dlg = new HistoryDialog();
            dlg.Show();
        }
    }
}