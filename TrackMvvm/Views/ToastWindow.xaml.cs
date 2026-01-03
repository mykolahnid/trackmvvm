using System;
using System.Windows;
using System.Windows.Threading;

namespace TrackMvvm.Views
{
    public partial class ToastWindow : Window
    {
        private DispatcherTimer _timer;

        public ToastWindow(string message, Window owner)
        {
            InitializeComponent();
            ToastMessage.Text = message;
            Owner = owner;

            // Position below the owner window
            PositionBelowOwner();

            // Auto-hide after 3 seconds
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            _timer.Tick += (s, e) =>
            {
                _timer.Stop();
                Close();
            };
            _timer.Start();
        }

        private void PositionBelowOwner()
        {
            if (Owner != null)
            {
                // Calculate position: centered horizontally below the owner window
                Left = Owner.Left + (Owner.Width - ActualWidth) / 2;
                Top = Owner.Top + Owner.Height + 5; // 5 pixels gap

                // Handle the case where ActualWidth/Height are not yet calculated
                Loaded += (s, e) =>
                {
                    Left = Owner.Left + (Owner.Width - ActualWidth) / 2;
                    Top = Owner.Top + Owner.Height + 5;
                };
            }
        }
    }
}
