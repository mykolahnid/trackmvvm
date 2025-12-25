using System;
using System.Linq;
using System.Threading.Tasks;
using Postgrest.Attributes;
using Postgrest.Models;
using TrackMvvm.Model;

namespace TrackMvvm.Services
{
    /// <summary>
    /// Database models for Supabase tables
    /// </summary>
    namespace Models
    {
        [Table("work_sessions")]
        public class WorkSessionDb : BaseModel
        {
            [PrimaryKey("id")]
            public Guid Id { get; set; }

            [Column("user_id")]
            public string UserId { get; set; } = string.Empty;

            [Column("session_date")]
            public DateOnly SessionDate { get; set; }

            [Column("created_at")]
            public DateTime CreatedAt { get; set; }

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }
        }

        [Table("task_times")]
        public class TaskTimeDb : BaseModel
        {
            [PrimaryKey("id")]
            public Guid Id { get; set; }

            [Column("session_id")]
            public Guid SessionId { get; set; }

            [Column("task_name")]
            public string TaskName { get; set; } = string.Empty;

            [Column("duration_seconds")]
            public decimal DurationSeconds { get; set; }

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }
        }

        [Table("active_tracking")]
        public class ActiveTrackingDb : BaseModel
        {
            [PrimaryKey("user_id")]
            public string UserId { get; set; } = string.Empty;

            [Column("active_task_id")]
            public Guid? ActiveTaskId { get; set; }

            [Column("device_id")]
            public string DeviceId { get; set; } = string.Empty;

            [Column("task_name")]
            public string? TaskName { get; set; }

            [Column("started_at")]
            public DateTime? StartedAt { get; set; }

            [Column("updated_at")]
            public DateTime UpdatedAt { get; set; }
        }
    }

    public class SupabaseService : ISupabaseService
    {
        private readonly Supabase.Client _client;
        private Action<string, string?>? _onActiveTrackingChanged;
        private bool _initialized = false;

        public bool IsAuthenticated => _client.Auth.CurrentUser != null;
        public string? UserId => _client.Auth.CurrentUser?.Id;

        public SupabaseService(string url, string anonKey)
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false // Disable for now, will add in Phase 4
            };

            _client = new Supabase.Client(url, anonKey, options);
            System.Diagnostics.Debug.WriteLine($"[SupabaseService] Instance created. HashCode: {this.GetHashCode()}");
        }

        private async Task EnsureInitializedAsync()
        {
            if (!_initialized)
            {
                await _client.InitializeAsync();
                _initialized = true;
            }
        }

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] AuthenticateAsync called");
                await EnsureInitializedAsync();
                var session = await _client.Auth.SignIn(email, password);
                bool success = session?.User != null;
                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] Authentication {(success ? "successful" : "failed")}. CurrentUser: {_client.Auth.CurrentUser?.Id}");
                return success;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] Authentication exception: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"Authentication error: {ex.Message}\n\nPlease check your credentials and Supabase configuration.",
                    "Error",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
                return false;
            }
        }

        public async Task SignOutAsync()
        {
            try
            {
                UnsubscribeFromActiveTracking();
                await _client.Auth.SignOut();
            }
            catch
            {
                // Ignore errors during sign out
            }
        }

        public async Task<bool> SyncSessionAsync(WorkSession session, string deviceId)
        {
            if (!IsAuthenticated || UserId == null)
                return false;

            try
            {
                await EnsureInitializedAsync();

                // Always use actual current date, not the date from the session object
                // (session might be from yesterday's XML file)
                var today = DateOnly.FromDateTime(DateTime.Today);

                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: session.Today.Date={session.Today.Date:yyyy-MM-dd}, DateOnly.Today={today:yyyy-MM-dd}");

                // Try to get existing session for today
                var existingSession = await _client
                    .From<Models.WorkSessionDb>()
                    .Where(s => s.UserId == UserId && s.SessionDate == today)
                    .Single();

                Guid sessionId;

                if (existingSession != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: Found existing session {existingSession.Id}");
                    // Update existing session
                    existingSession.UpdatedAt = DateTime.UtcNow;
                    await _client
                        .From<Models.WorkSessionDb>()
                        .Update(existingSession);
                    sessionId = existingSession.Id;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: No existing session found, inserting new");

                    // Insert new session (Id will be auto-generated by database)
                    var newSession = new Models.WorkSessionDb
                    {
                        UserId = UserId,
                        SessionDate = today,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    try
                    {
                        var insertResult = await _client
                            .From<Models.WorkSessionDb>()
                            .Insert(newSession);

                        if (insertResult?.Models?.Count == 0)
                            return false;

                        sessionId = insertResult!.Models[0].Id;
                        System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: Inserted new session {sessionId}");
                    }
                    catch (Exception insertEx)
                    {
                        System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: Insert failed ({insertEx.Message}), trying to fetch existing session");

                        // Insert failed (likely duplicate key), try to fetch the existing session again
                        existingSession = await _client
                            .From<Models.WorkSessionDb>()
                            .Where(s => s.UserId == UserId && s.SessionDate == today)
                            .Single();

                        if (existingSession != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: Found session after insert failure {existingSession.Id}");
                            existingSession.UpdatedAt = DateTime.UtcNow;
                            await _client
                                .From<Models.WorkSessionDb>()
                                .Update(existingSession);
                            sessionId = existingSession.Id;
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: Could not find or create session");
                            return false;
                        }
                    }
                }

                // Upsert all tasks
                foreach (var task in session.Tasks)
                {
                    // Try to get existing task
                    var existingTask = await _client
                        .From<Models.TaskTimeDb>()
                        .Where(t => t.SessionId == sessionId && t.TaskName == task.Name)
                        .Single();

                    if (existingTask != null)
                    {
                        // Update existing task
                        existingTask.DurationSeconds = (decimal)task.Duration;
                        existingTask.UpdatedAt = DateTime.UtcNow;
                        await _client
                            .From<Models.TaskTimeDb>()
                            .Update(existingTask);
                    }
                    else
                    {
                        // Insert new task (Id will be auto-generated)
                        var newTask = new Models.TaskTimeDb
                        {
                            SessionId = sessionId,
                            TaskName = task.Name,
                            DurationSeconds = (decimal)task.Duration,
                            UpdatedAt = DateTime.UtcNow
                        };

                        try
                        {
                            await _client
                                .From<Models.TaskTimeDb>()
                                .Insert(newTask);
                        }
                        catch (Exception taskInsertEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync: Task insert failed ({taskInsertEx.Message}), trying update");

                            // Insert failed, try to fetch and update
                            existingTask = await _client
                                .From<Models.TaskTimeDb>()
                                .Where(t => t.SessionId == sessionId && t.TaskName == task.Name)
                                .Single();

                            if (existingTask != null)
                            {
                                existingTask.DurationSeconds = (decimal)task.Duration;
                                existingTask.UpdatedAt = DateTime.UtcNow;
                                await _client
                                    .From<Models.TaskTimeDb>()
                                    .Update(existingTask);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] SyncSessionAsync exception: {ex.Message}");
                return false;
            }
        }

        public async Task<WorkSession?> GetTodaySessionAsync()
        {
            if (!IsAuthenticated || UserId == null)
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] GetTodaySessionAsync: Not authenticated or no UserId");
                return null;
            }

            try
            {
                await EnsureInitializedAsync();

                var today = DateOnly.FromDateTime(DateTime.Today);
                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] GetTodaySessionAsync: Querying for session on {today:yyyy-MM-dd}");

                // Get today's session
                var sessionResult = await _client
                    .From<Models.WorkSessionDb>()
                    .Where(s => s.UserId == UserId && s.SessionDate == today)
                    .Single();

                if (sessionResult == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] GetTodaySessionAsync: No session found for today");
                    return null;
                }

                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] GetTodaySessionAsync: Found session {sessionResult.Id}");

                // Get all tasks for this session
                var tasksResult = await _client
                    .From<Models.TaskTimeDb>()
                    .Where(t => t.SessionId == sessionResult.Id)
                    .Get();

                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] GetTodaySessionAsync: Found {tasksResult.Models.Count} tasks");

                var workSession = new WorkSession();
                workSession.Today = today.ToDateTime(TimeOnly.MinValue);

                foreach (var taskDb in tasksResult.Models)
                {
                    workSession.AddTask(taskDb.TaskName);
                    var task = workSession.Tasks.FirstOrDefault(t => t.Name == taskDb.TaskName);
                    if (task != null)
                    {
                        task.Duration = (double)taskDb.DurationSeconds;
                    }
                }

                return workSession;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SupabaseService {this.GetHashCode()}] GetTodaySessionAsync exception: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> StartTaskTrackingAsync(string taskName, string deviceId)
        {
            if (!IsAuthenticated || UserId == null)
                return false;

            try
            {
                await EnsureInitializedAsync();

                // Last-writer-wins: Just upsert active_tracking
                var activeTracking = new Models.ActiveTrackingDb
                {
                    UserId = UserId,
                    DeviceId = deviceId,
                    TaskName = taskName,
                    StartedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _client
                    .From<Models.ActiveTrackingDb>()
                    .Upsert(activeTracking);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> StopTaskTrackingAsync()
        {
            if (!IsAuthenticated || UserId == null)
                return false;

            try
            {
                await EnsureInitializedAsync();

                // Clear active tracking
                var activeTracking = new Models.ActiveTrackingDb
                {
                    UserId = UserId,
                    DeviceId = string.Empty,
                    TaskName = null,
                    StartedAt = null,
                    UpdatedAt = DateTime.UtcNow
                };

                await _client
                    .From<Models.ActiveTrackingDb>()
                    .Update(activeTracking);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateHeartbeatAsync()
        {
            if (!IsAuthenticated || UserId == null)
                return false;

            try
            {
                await EnsureInitializedAsync();

                // Get current active tracking and update timestamp
                var current = await _client
                    .From<Models.ActiveTrackingDb>()
                    .Where(x => x.UserId == UserId)
                    .Single();

                if (current != null)
                {
                    current.UpdatedAt = DateTime.UtcNow;
                    await _client
                        .From<Models.ActiveTrackingDb>()
                        .Update(current);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void SubscribeToActiveTracking(Action<string, string?> onChanged)
        {
            // Realtime will be implemented in Phase 4
            // For now, polling will be used
            _onActiveTrackingChanged = onChanged;
        }

        public void UnsubscribeFromActiveTracking()
        {
            _onActiveTrackingChanged = null;
        }
    }
}
