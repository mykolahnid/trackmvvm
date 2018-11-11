using GalaSoft.MvvmLight.Command;
using TrackMvvm.Model;

namespace TrackMvvm.ViewModel
{
    public class TaskTimeViewModel
    {
        public TaskTimeViewModel(TaskTime taskTime)
        {
            TaskTime = taskTime;
            ButtonCommand = new RelayCommand(taskTime.Start);
        }

        public TaskTime TaskTime { get; set; }

        public RelayCommand ButtonCommand { get; set; }
    }
}