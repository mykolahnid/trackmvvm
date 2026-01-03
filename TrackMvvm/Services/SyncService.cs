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
        System.Diagnostics.Debug.WriteLine($"[SyncService] Created with SupabaseService HashCode: {supabaseService.GetHashCode()}");
    }

    public async Task<bool> PushSessionAsync(WorkSession session)
    {
        if (!_supabaseService.IsAuthenticated)
            return false;

        return await _supabaseService.SyncSessionAsync(session, _deviceId);
    }

    public async Task<WorkSession?> PullSessionAsync()
    {
        System.Diagnostics.Debug.WriteLine($"[SyncService] PullSessionAsync called. IsAuthenticated: {_supabaseService.IsAuthenticated}");

        if (!_supabaseService.IsAuthenticated)
        {
            System.Diagnostics.Debug.WriteLine("[SyncService] Not authenticated, returning null");
            return null;
        }

        var result = await _supabaseService.GetTodaySessionAsync();
        System.Diagnostics.Debug.WriteLine($"[SyncService] GetTodaySessionAsync returned: {(result != null ? "Session with " + result.Tasks.Count + " tasks" : "null")}");
        return result;
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

    public async Task<(string DeviceId, string? TaskName, DateTime? UpdatedAt)?> GetActiveTrackingAsync()
    {
        if (!_supabaseService.IsAuthenticated)
            return null;

        return await _supabaseService.GetActiveTrackingAsync();
    }

    public async Task<bool> UpdateHeartbeatAsync()
    {
        if (!_supabaseService.IsAuthenticated)
            return false;

        return await _supabaseService.UpdateHeartbeatAsync();
    }

    /// <summary>
    /// Merges remote session into local session in-place.
    /// Updates local task durations and IsRemoteTracking based on remote data.
    /// - If remote > local: Update duration and mark as remote tracking
    /// - If remote <= local: Clear remote tracking indicator
    /// - Adds new tasks from remote that don't exist locally
    /// </summary>
    public static void MergeSessions(WorkSession local, WorkSession remote, bool isStartupSync = false)
    {
        // Update existing local tasks
        foreach (var localTask in local.Tasks)
        {
            var remoteTask = remote.Tasks.FirstOrDefault(t => t.Name == localTask.Name);
            if (remoteTask != null)
            {
                if (isStartupSync)
                {
                    // On startup: Use Math.Max (take larger value)
                    localTask.Duration = Math.Max(localTask.Duration, remoteTask.Duration);
                }
                else if (remoteTask.Duration > localTask.Duration)
                {
                    // During sync: If remote > local, server has newer data
                    localTask.Duration = remoteTask.Duration;
                    localTask.IsRemoteTracking = true;
                }
                else if (localTask.IsRemoteTracking)
                {
                    // Clear orange - server doesn't have newer data anymore
                    localTask.IsRemoteTracking = false;
                }
            }
        }

        // Add any new tasks from remote that we don't have locally
        foreach (var remoteTask in remote.Tasks)
        {
            if (!local.Tasks.Any(t => t.Name == remoteTask.Name))
            {
                local.AddTask(remoteTask.Name);
                var newTask = local.Tasks.First(t => t.Name == remoteTask.Name);
                newTask.Duration = remoteTask.Duration;
            }
        }
    }
}
