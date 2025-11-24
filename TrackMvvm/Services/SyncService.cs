using System;
using System.Linq;
using System.Threading.Tasks;
using TrackMvvm.Model;

namespace TrackMvvm.Services;

public class SyncService : ISyncService
{
    private readonly ISupabaseService _supabaseService;
    private readonly string _deviceId;

    public string DeviceId => _deviceId;

    public SyncService(ISupabaseService supabaseService)
    {
        _supabaseService = supabaseService;
        _deviceId = Environment.MachineName;
    }

    public async Task<bool> PushSessionAsync(WorkSession session)
    {
        if (!_supabaseService.IsAuthenticated)
            return false;

        return await _supabaseService.SyncSessionAsync(session, _deviceId);
    }

    public async Task<WorkSession?> PullSessionAsync()
    {
        if (!_supabaseService.IsAuthenticated)
            return null;

        return await _supabaseService.GetTodaySessionAsync();
    }

    public async Task<bool> StartTaskAsync(string taskName)
    {
        if (!_supabaseService.IsAuthenticated)
            return false;

        // Last-writer-wins: Just update active_tracking
        return await _supabaseService.StartTaskTrackingAsync(taskName, _deviceId);
    }

    public async Task<bool> StopTaskAsync()
    {
        if (!_supabaseService.IsAuthenticated)
            return false;

        return await _supabaseService.StopTaskTrackingAsync();
    }

    /// <summary>
    /// Merge remote session with local session (latest duration wins per task)
    /// </summary>
    public static WorkSession MergeSessions(WorkSession local, WorkSession remote)
    {
        var merged = new WorkSession { Today = local.Today };

        // Get all unique task names from both sessions
        var allTaskNames = local.Tasks.Select(t => t.Name)
            .Union(remote.Tasks.Select(t => t.Name))
            .Distinct();

        foreach (var taskName in allTaskNames)
        {
            merged.AddTask(taskName);
            var mergedTask = merged.Tasks.First(t => t.Name == taskName);

            var localTask = local.Tasks.FirstOrDefault(t => t.Name == taskName);
            var remoteTask = remote.Tasks.FirstOrDefault(t => t.Name == taskName);

            // Use the maximum duration (latest wins)
            var localDuration = localTask?.Duration ?? 0;
            var remoteDuration = remoteTask?.Duration ?? 0;

            mergedTask.Duration = Math.Max(localDuration, remoteDuration);
        }

        return merged;
    }
}
