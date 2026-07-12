using System;
using System.Threading.Tasks;
using TrackMvvm.Model;

namespace TrackMvvm.Services;

/// <summary>
/// Service for Supabase database operations and authentication
/// </summary>
public interface ISupabaseService
{
    /// <summary>
    /// Authenticate user with email and password
    /// </summary>
    Task<bool> AuthenticateAsync(string email, string password);

    /// <summary>
    /// Sign out the current user
    /// </summary>
    Task SignOutAsync();

    /// <summary>
    /// Check if user is currently authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Get the current user ID
    /// </summary>
    string UserId { get; }

    /// <summary>
    /// Sync work session to Supabase (upsert session and all tasks)
    /// </summary>
    Task<bool> SyncSessionAsync(WorkSession session, string deviceId);

    /// <summary>
    /// Get today's work session from Supabase
    /// </summary>
    Task<WorkSession> GetTodaySessionAsync();

    /// <summary>
    /// Start tracking a task (updates active_tracking table with last-writer-wins)
    /// </summary>
    Task<bool> StartTaskTrackingAsync(string taskName, string deviceId);

    /// <summary>
    /// Stop tracking (clears active_tracking), but only if the given device currently owns the lock.
    /// No-op (returns true) if another device owns it, so a device that isn't tracking anything
    /// can't clobber another device's active claim.
    /// </summary>
    Task<bool> StopTaskTrackingAsync(string deviceId);

    /// <summary>
    /// Update heartbeat timestamp (called every 30s while tracking)
    /// </summary>
    Task<bool> UpdateHeartbeatAsync();

    /// <summary>
    /// Get current active tracking state
    /// Returns tuple: (deviceId, taskName, updatedAt)
    /// </summary>
    Task<(string DeviceId, string TaskName, DateTime? UpdatedAt)?> GetActiveTrackingAsync();

    /// <summary>
    /// Subscribe to active tracking changes (Realtime)
    /// Callback parameters: deviceId (string), taskName (string)
    /// </summary>
    void SubscribeToActiveTracking(Action<string, string> onChanged);

    /// <summary>
    /// Unsubscribe from active tracking changes
    /// </summary>
    void UnsubscribeFromActiveTracking();
}
