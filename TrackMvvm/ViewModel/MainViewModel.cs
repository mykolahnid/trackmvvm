﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Threading;
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
                    foreach (var t in WorkSession.Tasks)
                    {
                        var taskTimeViewModel = new TaskTimeViewModel(t);
                        TasksCollection.Add(taskTimeViewModel);
                    }

                    StopCommand = new RelayCommand(WorkSession.Stop);
                    AddTaskCommand = new RelayCommand(OnAddTask);
                    CloseCommand = new RelayCommand(OnClosing);                    

                    saveSessionTimer.Tick += saveSessionTimer_Tick;
                    saveSessionTimer.Interval = TimeSpan.FromMinutes(1);
                    saveSessionTimer.Start();
                });

            HistoryCommand  = new RelayCommand(ShowHistory);
        }

        private void saveSessionTimer_Tick(object sender, EventArgs e)
        {
            _dataService.SaveWorkSession(WorkSession);
        }

        private void WorkSession_TaskStarted(string taskName)
        {
            Messenger.Default.Send(new NotificationMessage<string>(taskName, null), MessengerActions.TaskStarted);
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
            Messenger.Default.Send(
                new NotificationMessageWithCallback(null, (Action<string>) this.TaskNameReceived), MessengerActions.AddTaskDialog);
        }

        private void ShowHistory()
        {
            var workSessionHistory = _dataService.GetWorkSessionHistory();
            var historyBuilder = new StringBuilder();
            foreach (var workSession in workSessionHistory)
            {
                historyBuilder.AppendLine(workSession.Today.ToString("ddd dd MMM") + " " + workSession);
            }

            Messenger.Default.Send(new NotificationMessageWithCallback(null, historyBuilder.ToString(), (Action<bool>)this.OnHistoryDeleting), MessengerActions.ShowHistory);
        }

        private void OnHistoryDeleting(bool deleteHistory)
        {
            
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