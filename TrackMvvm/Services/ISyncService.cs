using System.Threading.Tasks;
using TrackMvvm.Model;

namespace TrackMvvm.Services;

public interface ISyncService
{
    /// <summary>
    /// Sync current session to Supabase
    /// </summary>
    Task<bool> PushSessionAsync(WorkSession session);

    /// <summary>
    /// Pull today's session from Supabase and merge with local
    /// </summary>
    Task<WorkSession?> PullSessionAsync();

    /// <summary>
    /// Start tracking a task (last-writer-wins)
    /// </summary>
    Task<bool> StartTaskAsync(string taskName);

    /// <summary>
    /// Stop tracking
    /// </summary>
    Task<bool> StopTaskAsync();

    /// <summary>
    /// Get device identifier
    /// </summary>
    string DeviceId { get; }
}
