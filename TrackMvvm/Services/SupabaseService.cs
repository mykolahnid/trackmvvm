using Supabase;
using Supabase.Gotrue;
using Supabase.Realtime;
using TrackMvvm.Model;
using static Supabase.Gotrue.Constants;

namespace TrackMvvm.Services;

/// <summary>
/// Database models for Supabase tables
/// </summary>
namespace Models
{
    [Supabase.Postgrest.Attributes.Table("work_sessions")]
    public class WorkSessionDb : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public Guid Id { get; set; }

        [Supabase.Postgrest.Attributes.Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("session_date")]
        public DateTime SessionDate { get; set; }

        [Supabase.Postgrest.Attributes.Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Supabase.Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    [Supabase.Postgrest.Attributes.Table("task_times")]
    public class TaskTimeDb : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("id")]
        public Guid Id { get; set; }

        [Supabase.Postgrest.Attributes.Column("session_id")]
        public Guid SessionId { get; set; }

        [Supabase.Postgrest.Attributes.Column("task_name")]
        public string TaskName { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("duration_seconds")]
        public decimal DurationSeconds { get; set; }

        [Supabase.Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }

    [Supabase.Postgrest.Attributes.Table("active_tracking")]
    public class ActiveTrackingDb : Supabase.Postgrest.Models.BaseModel
    {
        [Supabase.Postgrest.Attributes.PrimaryKey("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("active_task_id")]
        public Guid? ActiveTaskId { get; set; }

        [Supabase.Postgrest.Attributes.Column("device_id")]
        public string DeviceId { get; set; } = string.Empty;

        [Supabase.Postgrest.Attributes.Column("task_name")]
        public string? TaskName { get; set; }

        [Supabase.Postgrest.Attributes.Column("started_at")]
        public DateTime? StartedAt { get; set; }

        [Supabase.Postgrest.Attributes.Column("updated_at")]
        public DateTime UpdatedAt { get; set; }
    }
}

public class SupabaseService : ISupabaseService
{
    private readonly Client _client;
    private RealtimeChannel? _activeTrackingChannel;
    private Action<string, string?>? _onActiveTrackingChanged;

    public bool IsAuthenticated => _client.Auth.CurrentUser != null;
    public string? UserId => _client.Auth.CurrentUser?.Id;

    public SupabaseService(string url, string anonKey)
    {
        var options = new SupabaseOptions
        {
            AutoConnectRealtime = true
        };

        _client = new Client(url, anonKey, options);
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
        if (!IsAuthenticated || UserId == null)
            return;

        _onActiveTrackingChanged = onChanged;

        try
        {
            _activeTrackingChannel = _client.Realtime.Channel("active_tracking_changes");

            _activeTrackingChannel.Register(new RealtimeChannel.PostgresChangesOptions(
                schema: "public",
                table: "active_tracking",
                filter: $"user_id=eq.{UserId}"
            ));

            _activeTrackingChannel.OnPostgresChange += (sender, change) =>
            {
                if (change.Response?.Payload?.Data is Models.ActiveTrackingDb data)
                {
                    _onActiveTrackingChanged?.Invoke(data.DeviceId, data.TaskName);
                }
            };

            _activeTrackingChannel.Subscribe();
        }
        catch
        {
            // Failed to subscribe - will fall back to polling
        }
    }

    public void UnsubscribeFromActiveTracking()
    {
        if (_activeTrackingChannel != null)
        {
            _activeTrackingChannel.Unsubscribe();
            _activeTrackingChannel = null;
        }
        _onActiveTrackingChanged = null;
    }
}
