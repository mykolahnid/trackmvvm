using System;
using System.Collections.ObjectModel;
using System.Linq;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Messaging;
using TrackMvvm.Constants;
using TrackMvvm.Model;

namespace TrackMvvm.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        public ObservableCollection<TaskTimeViewModel> TasksCollection { get; set; } = new ObservableCollection<TaskTimeViewModel>();

        private WorkSession _workSession;

        public WorkSession WorkSession
        {
            get => _workSession;
            set => Set(ref _workSession, value);
        }

        public RelayCommand StopCommand { get; set; }
        public RelayCommand AddTaskCommand { get; set; }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            
            _dataService.GetWorkSession(
                (item, error) =>
                {
                    if (error != null)
                    {
                        return;
                    }

                    WorkSession = item;
                    WorkSession.TaskAdded += this.WorkSession_TaskAdded;
                    foreach (var t in WorkSession.Tasks)
                    {
                        var taskTimeViewModel = new TaskTimeViewModel(t);
                        TasksCollection.Add(taskTimeViewModel);
                    }

                    StopCommand = new RelayCommand(WorkSession.Stop);
                    AddTaskCommand = new RelayCommand(OnAddTask);
                });
        }

        private void WorkSession_TaskAdded(object sender, TaskTime addedTaskTime)
        {
            TasksCollection.Add(new TaskTimeViewModel(addedTaskTime));
        }

        private void OnAddTask()
        {
            Messenger.Default.Send(
                new NotificationMessageWithCallback(null, (Action<string>) this.TaskNameReceived), MessengerActions.AddTaskDialog);
        }

        private void TaskNameReceived(string name)
        {
            WorkSession.AddTask(name);
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}