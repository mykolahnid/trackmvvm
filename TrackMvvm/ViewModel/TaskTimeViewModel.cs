using System.Windows.Media;
using GalaSoft.MvvmLight.Command;
using TrackMvvm.Model;

namespace TrackMvvm.ViewModel
{
    public class TaskTimeViewModel
    {
        public TaskTimeViewModel(TaskTime taskTime)
        {
            Name = taskTime.Name;
            Duration = taskTime.Duration;
            Color = Colors.Black;
            IsActive = taskTime.IsActive;
        }

        public string Name { get; set; }

        public double Duration { get; set; }

        public Color Color { get; set; }

        public bool IsActive { get; set; }

        public string DurationHours => (Duration / 60.0 / 60.0).ToString("0.0");

        public RelayCommand ButtonCommand { get; set; }
    }
}