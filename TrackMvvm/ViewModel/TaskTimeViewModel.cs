using GalaSoft.MvvmLight.Command;
using TrackMvvm.Model;

namespace TrackMvvm.ViewModel
{
    public class TaskTimeViewModel
    {
        public TaskTimeViewModel(TaskTime taskTime)
        {
            TaskTime = taskTime;
            StartCommand = new RelayCommand(taskTime.Start);
            RemoveCommand = new RelayCommand(taskTime.Remove);
        }

        public TaskTime TaskTime { get; set; }

        public RelayCommand StartCommand { get; set; }

        public RelayCommand RemoveCommand { get; set; }
    }
}