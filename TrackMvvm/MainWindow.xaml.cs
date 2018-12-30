﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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
            Messenger.Default.Register<NotificationMessage<string>>(this, MessengerActions.TaskStarted, OnTaskStarted);
            Messenger.Default.Register<NotificationMessageWithCallback>(this, MessengerActions.ShowHistory, OnShowHistory);

            Closing += (s, e) => ViewModelLocator.Cleanup();
        }

        private void OnShowHistory(NotificationMessageWithCallback message)
        {
            var text = message.Notification;
            var messageBoxResult = MessageBox.Show(text, "History", MessageBoxButton.YesNo);
            if (messageBoxResult == MessageBoxResult.Yes)
            {
                message.Execute(true);
            }
        }

        private void OnTaskStarted(NotificationMessage<string> message)
        {
            var text = message.Content;

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


        private void AskForTaskName(NotificationMessageWithCallback message)
        {
            AddTaskDialog dialog = new AddTaskDialog();
            dialog.DataContext = new AddTaskViewModel();
            if (dialog.ShowDialog() == true)
            {
                message.Execute((dialog.DataContext as AddTaskViewModel).TaskName);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            HistoryDialog dlg = new HistoryDialog();
            dlg.Show();
        }
    }
}