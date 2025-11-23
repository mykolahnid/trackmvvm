using System;
using System.Collections.Generic;
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
            public DateTime SessionDate { get; set; }

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

        public bool IsAuthenticated => _client.Auth.CurrentUser != null;
        public string? UserId => _client.Auth.CurrentUser?.Id;

        public SupabaseService(string url, string anonKey)
        {
            var options = new Supabase.SupabaseOptions
            {
                AutoConnectRealtime = false // Disable for now, will add in Phase 4
            };

            _client = new Supabase.Client(url, anonKey, options);
        }

        public async Task<bool> AuthenticateAsync(string email, string password)
        {
            try
            {
                var session = await _client.Auth.SignIn(email, password);
                return session?.User != null;
            }
            catch
            {
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
                // Upsert work_session
                var sessionDb = new Models.WorkSessionDb
                {
                    UserId = UserId,
                    SessionDate = session.Today.Date,
                    UpdatedAt = DateTime.UtcNow
                };

                var sessionResult = await _client
                    .From<Models.WorkSessionDb>()
                    .Upsert(sessionDb);

                if (sessionResult?.Models?.Count == 0)
                    return false;

                var sessionId = sessionResult!.Models[0].Id;

                // Upsert all tasks
                foreach (var task in session.Tasks)
                {
                    var taskDb = new Models.TaskTimeDb
                    {
                        SessionId = sessionId,
                        TaskName = task.Name,
                        DurationSeconds = (decimal)task.Duration,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _client
                        .From<Models.TaskTimeDb>()
                        .Upsert(taskDb);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<WorkSession?> GetTodaySessionAsync()
        {
            if (!IsAuthenticated || UserId == null)
                return null;

            try
            {
                var today = DateTime.Today;

                // Get today's session
                var sessionResult = await _client
                    .From<Models.WorkSessionDb>()
                    .Where(s => s.UserId == UserId && s.SessionDate == today)
                    .Single();

                if (sessionResult == null)
                    return null;

                // Get all tasks for this session
                var tasksResult = await _client
                    .From<Models.TaskTimeDb>()
                    .Where(t => t.SessionId == sessionResult.Id)
                    .Get();

                var workSession = new WorkSession();
                workSession.Today = today;

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
            catch
            {
                return null;
            }
        }

        public async Task<bool> StartTaskTrackingAsync(string taskName, string deviceId)
        {
            if (!IsAuthenticated || UserId == null)
                return false;

            try
            {
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
                // Just update the timestamp
                var update = new Dictionary<string, object>
                {
                    { "updated_at", DateTime.UtcNow }
                };

                await _client
                    .From<Models.ActiveTrackingDb>()
                    .Where(x => x.UserId == UserId)
                    .Update(update);

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
