using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using System;
using System.Collections.ObjectModel;
using System.Linq;
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
        private readonly Services.ISyncService? _syncService;

        public ObservableCollection<TaskTimeViewModel> TasksCollection { get; set; } = new ObservableCollection<TaskTimeViewModel>();

        [ObservableProperty]
        private WorkSession _workSession;

        public RelayCommand StopCommand { get; set; }
        public RelayCommand AddTaskCommand { get; set; }
        public RelayCommand HistoryCommand { get; set; }

        private readonly DispatcherTimer saveSessionTimer = new DispatcherTimer();

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService, Services.ISyncService syncService = null)
        {
            _dataService = dataService;
            _syncService = syncService;

            // Listen for authentication completion to trigger sync
            WeakReferenceMessenger.Default.Register<AuthenticationCompletedMessage>(this, async (r, m) =>
            {
                if (m.Success && _syncService != null && WorkSession != null)
                {
                    await PullAndMergeRemoteDataAsync();
                }
            });

            _dataService.GetWorkSession(
                async (item, error) =>
                {
                    if (error != null)
                    {
                        return;
                    }

                    WorkSession = item;
                    WorkSession.TaskAdded += this.WorkSession_TaskAdded;
                    WorkSession.TaskStarted += this.WorkSession_TaskStarted;
                    WorkSession.TaskRemoved += this.WorkSession_TaskRemoved;

                    // Don't pull here - wait for authentication to complete
                    // Pull will happen when AuthenticationCompletedMessage is received

                    foreach (var t in WorkSession.Tasks)
                    {
                        var taskTimeViewModel = new TaskTimeViewModel(t);
                        TasksCollection.Add(taskTimeViewModel);
                    }

                    StopCommand = new RelayCommand(OnStop);
                    AddTaskCommand = new RelayCommand(OnAddTask);

                    saveSessionTimer.Tick += saveSessionTimer_Tick;
                    saveSessionTimer.Interval = TimeSpan.FromMinutes(1);
                    saveSessionTimer.Start();
                });

            HistoryCommand = new RelayCommand(ShowHistory);
        }

        private async System.Threading.Tasks.Task PullAndMergeRemoteDataAsync()
        {
            if (_syncService == null || WorkSession == null)
                return;

            try
            {
                var remoteSession = await _syncService.PullSessionAsync();
                if (remoteSession != null)
                {
                    var merged = Services.SyncService.MergeSessions(WorkSession, remoteSession);

                    // Update existing tasks with merged durations
                    foreach (var mergedTask in merged.Tasks)
                    {
                        var existingTask = WorkSession.Tasks.FirstOrDefault(t => t.Name == mergedTask.Name);
                        if (existingTask != null)
                        {
                            existingTask.Duration = mergedTask.Duration;
                        }
                        else
                        {
                            // Add new task from remote
                            WorkSession.AddTask(mergedTask.Name);
                            var newTask = WorkSession.Tasks.First(t => t.Name == mergedTask.Name);
                            newTask.Duration = mergedTask.Duration;

                            var taskTimeViewModel = new TaskTimeViewModel(newTask);
                            TasksCollection.Add(taskTimeViewModel);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine("Successfully pulled and merged remote session after authentication");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to pull remote session: {ex.Message}");
            }
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

        private async void saveSessionTimer_Tick(object sender, EventArgs e)
        {
            _dataService.SaveWorkSession(WorkSession);

            // Push to Supabase if sync is available
            if (_syncService != null)
            {
                try
                {
                    await _syncService.PushSessionAsync(WorkSession);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to push session: {ex.Message}");
                }
            }
        }

        private async void WorkSession_TaskStarted(string taskName)
        {
            WeakReferenceMessenger.Default.Send(new TaskStartedMessage(taskName));

            // Update active tracking with last-writer-wins
            if (_syncService != null && !string.IsNullOrEmpty(taskName))
            {
                try
                {
                    await _syncService.StartTaskAsync(taskName);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start task tracking: {ex.Message}");
                }
            }
        }

        private void WorkSession_TaskAdded(object sender, TaskTime addedTaskTime)
        {
            TasksCollection.Add(new TaskTimeViewModel(addedTaskTime));
        }

        public async System.Threading.Tasks.Task OnClosingAsync()
        {
            System.Diagnostics.Debug.WriteLine("[MainViewModel] OnClosingAsync called");
            _dataService.SaveWorkSession(WorkSession);

            // Push to Supabase if sync is available
            if (_syncService != null)
            {
                try
                {
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Pushing session to Supabase on close...");
                    await _syncService.PushSessionAsync(WorkSession);
                    System.Diagnostics.Debug.WriteLine("[MainViewModel] Successfully pushed session on close");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[MainViewModel] Failed to push session on close: {ex.Message}");
                }
            }
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

        private async void OnStop()
        {
            WeakReferenceMessenger.Default.Send(new TaskStartedMessage(""));
            WorkSession.Stop();

            // Clear active tracking
            if (_syncService != null)
            {
                try
                {
                    await _syncService.StopTaskAsync();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to stop task tracking: {ex.Message}");
                }
            }
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