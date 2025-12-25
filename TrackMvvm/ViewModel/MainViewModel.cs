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
        private DateTime? _lastHeartbeatSent = null;

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
                    saveSessionTimer.Interval = TimeSpan.FromSeconds(30); // Check conflicts + heartbeat every 30s
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
                            // Note: WorkSession.AddTask fires TaskAdded event which updates TasksCollection
                            WorkSession.AddTask(mergedTask.Name);
                            var newTask = WorkSession.Tasks.First(t => t.Name == mergedTask.Name);
                            newTask.Duration = mergedTask.Duration;
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

        /// <summary>
        /// Performs a full sync cycle: pull remote data, merge with Math.Max, push back
        /// </summary>
        private async System.Threading.Tasks.Task SyncWithMergeAsync(string context = "sync")
        {
            if (_syncService == null || WorkSession == null)
                return;

            try
            {
                System.Diagnostics.Debug.WriteLine($"[{context}] Starting sync with merge...");

                // Pull remote data first
                var remoteSession = await _syncService.PullSessionAsync();
                if (remoteSession != null)
                {
                    // Merge with Math.Max strategy
                    var merged = Services.SyncService.MergeSessions(WorkSession, remoteSession);

                    // Update local tasks with merged durations
                    foreach (var mergedTask in merged.Tasks)
                    {
                        var existingTask = WorkSession.Tasks.FirstOrDefault(t => t.Name == mergedTask.Name);
                        if (existingTask != null && existingTask.Duration != mergedTask.Duration)
                        {
                            System.Diagnostics.Debug.WriteLine($"[{context}] Task '{mergedTask.Name}': {existingTask.Duration}s -> {mergedTask.Duration}s");
                            existingTask.Duration = mergedTask.Duration;
                        }
                    }
                }

                // Now push merged data
                await _syncService.PushSessionAsync(WorkSession);
                System.Diagnostics.Debug.WriteLine($"[{context}] Sync with merge completed");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[{context}] Failed to sync with merge: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if another device has started tracking after our last heartbeat
        /// Returns true if conflict detected (another device took over)
        /// </summary>
        private async System.Threading.Tasks.Task<bool> CheckForActiveTrackingConflictAsync()
        {
            // Check if we're currently tracking a task
            var isTracking = WorkSession?.Tasks?.Any(t => t.IsActive) == true;
            if (_syncService == null || !isTracking)
                return false; // No conflict if we're not tracking

            try
            {
                var tracking = await _syncService.GetActiveTrackingAsync();

                if (tracking == null)
                    return false; // No active tracking in database

                var (deviceId, taskName, updatedAt) = tracking.Value;

                // Get our device ID
                var ourDeviceId = Environment.MachineName;

                // Check if another device has updated the tracking after our last heartbeat
                if (deviceId != ourDeviceId && updatedAt.HasValue && _lastHeartbeatSent.HasValue)
                {
                    if (updatedAt.Value > _lastHeartbeatSent.Value)
                    {
                        System.Diagnostics.Debug.WriteLine($"[Conflict] Another device '{deviceId}' started task '{taskName}' at {updatedAt.Value:HH:mm:ss}");
                        System.Diagnostics.Debug.WriteLine($"[Conflict] Our last heartbeat was at {_lastHeartbeatSent.Value:HH:mm:ss}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Conflict] Failed to check active tracking: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Stops local task and shows notification when another device takes over
        /// </summary>
        private async System.Threading.Tasks.Task StopLocalTaskDueToConflictAsync()
        {
            try
            {
                var tracking = await _syncService?.GetActiveTrackingAsync();
                if (tracking == null)
                    return;

                var (deviceId, taskName, _) = tracking.Value;

                System.Diagnostics.Debug.WriteLine($"[Conflict] Stopping local task. Device '{deviceId}' is now tracking '{taskName}'");

                // Stop local task (don't clear remote tracking - other device owns it now)
                WeakReferenceMessenger.Default.Send(new TaskStartedMessage(""));
                WorkSession.Stop();

                // Show notification to user
                System.Windows.MessageBox.Show(
                    $"Your task was stopped because device '{deviceId}' started tracking '{taskName}'.",
                    "Task Stopped by Another Device",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Conflict] Failed to stop local task: {ex.Message}");
            }
        }

        private async void saveSessionTimer_Tick(object sender, EventArgs e)
        {
            // STEP 1: Check for conflicts FIRST (before sending heartbeat)
            bool conflictDetected = await CheckForActiveTrackingConflictAsync();

            if (conflictDetected)
            {
                // Another device took over - stop local task and show notification
                await StopLocalTaskDueToConflictAsync();
                return; // Don't send heartbeat or save if we were stopped by another device
            }

            // STEP 2: Send heartbeat if we're currently tracking (only if no conflict)
            var isTracking = WorkSession?.Tasks?.Any(t => t.IsActive) == true;
            if (isTracking && _syncService != null)
            {
                try
                {
                    await _syncService.UpdateHeartbeatAsync();
                    _lastHeartbeatSent = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine("[Timer] Heartbeat sent");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Timer] Failed to send heartbeat: {ex.Message}");
                }
            }

            // STEP 3: Save and sync (every 30s)
            _dataService.SaveWorkSession(WorkSession);
            await SyncWithMergeAsync("Timer");
        }

        private async void WorkSession_TaskStarted(string taskName)
        {
            WeakReferenceMessenger.Default.Send(new TaskStartedMessage(taskName));

            // Update active tracking with last-writer-wins
            if (_syncService != null && !string.IsNullOrEmpty(taskName))
            {
                try
                {
                    // First, check if we're overtaking another device's active task
                    var previousTracking = await _syncService.GetActiveTrackingAsync();

                    // Claim the task (overwrites previous device)
                    await _syncService.StartTaskAsync(taskName);
                    _lastHeartbeatSent = DateTime.UtcNow;
                    System.Diagnostics.Debug.WriteLine($"[Task Start] Started tracking '{taskName}', heartbeat timestamp set");

                    // If we overtook another device with a fresh timestamp, capture their in-flight time
                    if (previousTracking.HasValue)
                    {
                        var (prevDeviceId, prevTaskName, prevUpdatedAt) = previousTracking.Value;
                        var ourDeviceId = Environment.MachineName;

                        // Check if another device was tracking the same task recently (< 30 seconds)
                        if (prevDeviceId != ourDeviceId &&
                            prevTaskName == taskName &&
                            prevUpdatedAt.HasValue)
                        {
                            var timeSinceLastUpdate = DateTime.UtcNow - prevUpdatedAt.Value;

                            if (timeSinceLastUpdate.TotalSeconds < 30)
                            {
                                System.Diagnostics.Debug.WriteLine($"[Task Start] Overtaking device '{prevDeviceId}' - capturing in-flight time");

                                // Pull the latest remote session to get the task's duration
                                var remoteSession = await _syncService.PullSessionAsync();
                                if (remoteSession != null)
                                {
                                    var remoteTask = remoteSession.Tasks.FirstOrDefault(t => t.Name == taskName);
                                    if (remoteTask != null)
                                    {
                                        // Estimate their accumulated time: remote_duration + time_since_last_update
                                        var estimatedRemoteDuration = remoteTask.Duration + timeSinceLastUpdate.TotalSeconds;
                                        var localTask = WorkSession.Tasks.FirstOrDefault(t => t.Name == taskName);

                                        if (localTask != null)
                                        {
                                            var localDuration = localTask.Duration;
                                            var capturedDuration = Math.Max(localDuration, estimatedRemoteDuration);

                                            System.Diagnostics.Debug.WriteLine($"[Task Start] In-flight capture: local={localDuration:F0}s, remote={remoteTask.Duration:F0}s + {timeSinceLastUpdate.TotalSeconds:F0}s = estimated={estimatedRemoteDuration:F0}s, using={capturedDuration:F0}s");

                                            // Set the task to start from the higher value
                                            localTask.Duration = capturedDuration;
                                        }
                                    }
                                }
                            }
                        }
                    }
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

            // Sync with merge and clear active tracking
            if (_syncService != null)
            {
                try
                {
                    // Sync with Math.Max merge
                    await SyncWithMergeAsync("Close");

                    // Clear active tracking when app closes (user is no longer tracking anything)
                    System.Diagnostics.Debug.WriteLine("[Close] Clearing active tracking...");
                    await _syncService.StopTaskAsync();
                    System.Diagnostics.Debug.WriteLine("[Close] Successfully cleared active tracking");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Close] Failed to sync: {ex.Message}");
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