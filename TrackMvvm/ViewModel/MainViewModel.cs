using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using TrackMvvm.Model;

namespace TrackMvvm.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        private readonly IDataService _dataService;

        public ObservableCollection<TaskTimeViewModel> TasksCollection { get; set; } = new ObservableCollection<TaskTimeViewModel>();

        [ObservableProperty]
        private WorkSession _workSession;

        public RelayCommand StopCommand { get; set; }
        public RelayCommand AddTaskCommand { get; set; }
        public RelayCommand CloseCommand { get; set; }
        public RelayCommand HistoryCommand { get; set; }

        private readonly DispatcherTimer saveSessionTimer = new DispatcherTimer();

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
                    WorkSession.TaskStarted += this.WorkSession_TaskStarted;
                    WorkSession.TaskRemoved += this.WorkSession_TaskRemoved;
                    foreach (var t in WorkSession.Tasks)
                    {
                        var taskTimeViewModel = new TaskTimeViewModel(t);
                        TasksCollection.Add(taskTimeViewModel);
                    }

                    StopCommand = new RelayCommand(OnStop);
                    AddTaskCommand = new RelayCommand(OnAddTask);
                    CloseCommand = new RelayCommand(OnClosing);

                    saveSessionTimer.Tick += saveSessionTimer_Tick;
                    saveSessionTimer.Interval = TimeSpan.FromMinutes(1);
                    saveSessionTimer.Start();
                });

            HistoryCommand = new RelayCommand(ShowHistory);
        }

        private void WorkSession_TaskRemoved(string taskName)
        {
            for (int i = 0; i < TasksCollection.Count; i++)
            {
                if (TasksCollection[i].TaskTime.Name == taskName)
                {
                    TasksCollection.RemoveAt(i);
                    break;
                }
            }
        }

        private void saveSessionTimer_Tick(object sender, EventArgs e)
        {
            _dataService.SaveWorkSession(WorkSession);
        }

        private void WorkSession_TaskStarted(string taskName)
        {
            WeakReferenceMessenger.Default.Send(new TaskStartedMessage(taskName));
        }

        private void WorkSession_TaskAdded(object sender, TaskTime addedTaskTime)
        {
            TasksCollection.Add(new TaskTimeViewModel(addedTaskTime));
        }

        private void OnClosing()
        {
            _dataService.SaveWorkSession(WorkSession);
        }

        private void OnAddTask()
        {
            System.Diagnostics.Debug.WriteLine("Sending message...");
            WeakReferenceMessenger.Default.Send(
                new AddTaskDialogMessage(taskName =>
                {
                    // Handle the result of the dialog
                    System.Diagnostics.Debug.WriteLine($"Task name received: {taskName}");
                    TaskNameReceived(taskName);
                }),
                "MainWindow");
        }

        private void OnStop()
        {
            WeakReferenceMessenger.Default.Send(new TaskStartedMessage(""));
            WorkSession.Stop();
        }

        private void ShowHistory()
        {
            _dataService.SaveWorkSession(WorkSession);
            var workSessionHistory = _dataService.GetWorkSessionHistory();
            WorkSessionHistoryViewModel historyViewModel = new WorkSessionHistoryViewModel();
            historyViewModel.SetWorkSessionHistory(workSessionHistory);

            WeakReferenceMessenger.Default.Send(new ShowHistoryMessage(historyViewModel, this.OnHistoryDeleting));
        }

        private void OnHistoryDeleting(bool deleteHistory)
        {
            if (deleteHistory)
            {
                _dataService.DeleteHistory();
            }
        }

        private void TaskNameReceived(string name)
        {
            WorkSession.AddTask(name);
        }

        // Optional: Override for cleanup if needed
        // protected override void OnActivated()
        // {
        //     base.OnActivated();
        // }

        // protected override void OnDeactivated()
        // {
        //     // Clean up if needed
        //     base.OnDeactivated();
        // }
    }
}