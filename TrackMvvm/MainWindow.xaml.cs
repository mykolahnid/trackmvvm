using System.Windows;
using GalaSoft.MvvmLight.Messaging;
using TrackMvvm.Constants;
using TrackMvvm.ViewModel;
using TrackMvvm.Views;

namespace TrackMvvm
{
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

            Messenger.Default.Register<NotificationMessageWithCallback>(this, MessengerActions.AddTaskDialog, AskForTaskName);

            Closing += (s, e) => ViewModelLocator.Cleanup();
        }

        private void AskForTaskName(NotificationMessageWithCallback obj)
        {
            AddTaskDialog dialog = new AddTaskDialog();
            dialog.DataContext = new AddTaskViewModel();
            if (dialog.ShowDialog() == true)
            {
                obj.Execute((dialog.DataContext as AddTaskViewModel).TaskName);
            }
        }
    }
}