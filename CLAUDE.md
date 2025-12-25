# TrackMVVM - Supabase Sync Implementation

## Project Overview

**Goal**: Add Supabase cloud synchronization to a .NET 10.0 WPF time tracking application to enable working across two PCs with last-writer-wins conflict resolution.

**User Requirements**:
- Single-user setup (no complex multi-user auth needed)
- Only one project tracking at a time across all devices
- Last-writer-wins strategy (when PC2 starts a task, PC1 should auto-stop)
- Free tier Supabase (no cost)
- Public GitHub repo with gitignored API keys
- Local XML files remain primary storage, Supabase is for sync only

## Current Status: Phase 3 Complete ✅

### Phase 1: Foundation ✅ (Completed)
- ✅ Supabase client integration (supabase-csharp 0.16.2)
- ✅ Configuration management (appsettings.local.json, gitignored)
- ✅ Windows Credential Manager for secure credential storage
- ✅ Login dialog with "Remember Me" functionality
- ✅ Authentication flow in MainWindow.Loaded event
- ✅ ISupabaseService and SupabaseService implementation
- ✅ Database schema with RLS policies
- ✅ Tables: work_sessions, task_times, active_tracking

### Phase 2: Basic Sync ✅ (Completed)
- ✅ ISyncService and SyncService for orchestrating sync
- ✅ Pull on startup: Fetches remote session and merges with local
- ✅ Push on save: Every 30s (timer) and on app close
- ✅ Last-writer-wins on task start: Updates active_tracking table
- ✅ Stop tracking: Clears active_tracking when task stopped
- ✅ Session merging: Uses Math.Max for duration conflicts

### Phase 3: Active Task Lock ✅ (Completed)
**Goal**: Enforce last-writer-wins in real-time
- ✅ Add periodic heartbeat (every 30s) to update active_tracking.updated_at
- ✅ Add conflict detection to check if another device took over
- ✅ Detect if another device started a task and auto-stop local task
- ✅ Show notification when task is stopped by another device
- ✅ In-flight time capture when overtaking tasks (prevents losing 0-30s of work)
- ✅ Timezone-aware timestamp comparisons (ToUniversalTime for cross-timezone support)

### Phase 4: Realtime + Polish 🔄 (Future)
**Goal**: Instant updates and offline support
- ⏳ Subscribe to active_tracking table changes (Realtime)
- ⏳ Instant UI updates when remote device starts/stops task
- ⏳ Offline queue for failed sync operations
- ⏳ Conflict resolution UI if needed
- ⏳ Add sync status indicator in UI

## Key Files

### Created Files

**Configuration & Security**:
- `TrackMvvm/appsettings.local.json.template` - Template for Supabase config (gitignored actual file)
- `TrackMvvm/Utilities/CredentialStorage.cs` - Windows Credential Manager wrapper
- `.gitignore` - Added rules for local config files

**Authentication UI**:
- `TrackMvvm/ViewModel/LoginViewModel.cs` - MVVM view model for login
- `TrackMvvm/Views/LoginDialog.xaml` - Login UI
- `TrackMvvm/Views/LoginDialog.xaml.cs` - Validation logic in code-behind

**Supabase Integration**:
- `TrackMvvm/Services/ISupabaseService.cs` - Interface for Supabase operations
- `TrackMvvm/Services/SupabaseService.cs` - Implementation with DB models
  - Models: WorkSessionDb, TaskTimeDb, ActiveTrackingDb
  - Methods: AuthenticateAsync, SyncSessionAsync, Start/StopTaskTrackingAsync, UpdateHeartbeatAsync
- `TrackMvvm/Services/ISyncService.cs` - Interface for sync orchestration
- `TrackMvvm/Services/SyncService.cs` - Orchestrates between DataService and SupabaseService
  - DeviceId: Environment.MachineName
  - MergeSessions: Static method using Math.Max for durations

**Database**:
- `supabase-setup.sql` - Complete schema with tables, indexes, RLS, triggers
- `SUPABASE_SETUP.md` - Step-by-step setup guide

### Modified Files

**Project Configuration**:
- `TrackMvvm/TrackMvvm.csproj` - Added NuGet packages (supabase-csharp 0.16.2, CredentialManagement 1.0.2)

**Application Startup**:
- `TrackMvvm/App.xaml` - Restored StartupUri="MainWindow.xaml"
- `TrackMvvm/App.xaml.cs` - Empty (auth moved to MainWindow)
- `TrackMvvm/MainWindow.xaml.cs` - MainWindow_Loaded event handles authentication

**Dependency Injection**:
- `TrackMvvm/ViewModel/ViewModelLocator.cs` - Registers ISupabaseService and ISyncService

**Core Logic** (MOST IMPORTANT):
- `TrackMvvm/ViewModel/MainViewModel.cs` - **Integrated sync at all key points**:
  - Line 30: _lastHeartbeatSent timestamp tracking
  - Line 74: Timer interval changed to 30s (from 60s)
  - Line 82-119: Pull and merge on startup (after authentication)
  - Line 136-172: SyncWithMergeAsync (pull → merge → push pattern)
  - Line 177-220: CheckForActiveTrackingConflictAsync (conflict detection)
  - Line 225-252: StopLocalTaskDueToConflictAsync (handles takeover notification)
  - Line 254-285: Timer tick (conflict check → heartbeat → save/sync)
  - Line 287-358: WorkSession_TaskStarted (claim task + in-flight time capture)
  - Line 366-389: OnClosingAsync (sync + clear tracking on app close)
  - Line 404-421: OnStop (clear tracking when user stops task)

## Database Schema

```sql
-- work_sessions: One record per day per user
CREATE TABLE work_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES auth.users NOT NULL,
    session_date DATE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE(user_id, session_date)
);

-- task_times: Tasks within each session
CREATE TABLE task_times (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id UUID REFERENCES work_sessions(id) ON DELETE CASCADE,
    task_name TEXT NOT NULL,
    duration_seconds INTEGER DEFAULT 0,
    updated_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE(session_id, task_name)
);

-- active_tracking: Current active task (one per user)
CREATE TABLE active_tracking (
    user_id UUID PRIMARY KEY REFERENCES auth.users,
    active_task_id UUID,
    device_id TEXT NOT NULL,
    task_name TEXT,
    started_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ DEFAULT now()
);
```

**Indexes**:
- work_sessions: (user_id, session_date)
- task_times: (session_id), (task_name)
- active_tracking: (device_id), (updated_at)

**RLS Policies**: Users can only access their own data

**Realtime**: Enabled on active_tracking table (ready for Phase 4)

## Authentication Flow

1. App starts → MainWindow.Loaded fires
2. Check CredentialStorage for saved credentials
3. If found → Auto-login with SupabaseService.AuthenticateAsync()
4. If not found → Show LoginDialog
5. User enters email/password → Validate → Authenticate
6. If "Remember Me" checked → Save to CredentialStorage
7. If auth fails or cancelled → Shutdown app
8. If auth succeeds → Show main window

**Important**: Authentication moved to MainWindow.Loaded because async void OnStartup caused premature app shutdown.

## Sync Flow

### On Startup (MainViewModel.cs:82-119)
```csharp
// 1. Load local WorkSession from XML
// 2. Wait for authentication to complete (AuthenticationCompletedMessage)
// 3. Pull remote session from Supabase
var remoteSession = await _syncService.PullSessionAsync();
// 4. Merge using Math.Max for durations
var merged = SyncService.MergeSessions(WorkSession, remoteSession);
// 5. Update existing tasks with merged durations
// 6. Add new tasks from remote
```

### Every 30 Seconds (Timer Tick - MainViewModel.cs:254-285)
```csharp
// STEP 1: Check for conflicts FIRST (before sending heartbeat)
bool conflictDetected = await CheckForActiveTrackingConflictAsync();
if (conflictDetected) {
    // Another device took over - stop local task and show notification
    await StopLocalTaskDueToConflictAsync();
    return;
}

// STEP 2: Send heartbeat if we're currently tracking (only if no conflict)
if (isTracking) {
    await _syncService.UpdateHeartbeatAsync();
    _lastHeartbeatSent = DateTime.UtcNow;
}

// STEP 3: Save and sync (pull → merge → push)
_dataService.SaveWorkSession(WorkSession);
await SyncWithMergeAsync("Timer");
```

### On Task Start (MainViewModel.cs:287-358)
```csharp
// 1. Send UI message
WeakReferenceMessenger.Default.Send(new TaskStartedMessage(taskName));

// 2. Check if we're overtaking another device's active task
var previousTracking = await _syncService.GetActiveTrackingAsync();

// 3. Claim the task (overwrites previous device - last-writer-wins)
await _syncService.StartTaskAsync(taskName);
_lastHeartbeatSent = DateTime.UtcNow;

// 4. If we overtook another device with a fresh timestamp (<30s), capture in-flight time
if (previousTracking.HasValue && prevUpdatedAt < 30s ago) {
    // Pull latest remote session
    var remoteSession = await _syncService.PullSessionAsync();
    // Estimate in-flight time: remote_duration + time_since_last_update
    var estimatedRemoteDuration = remoteTask.Duration + timeSinceLastUpdate.TotalSeconds;
    // Use max(local, estimated_remote) as starting duration
    localTask.Duration = Math.Max(localDuration, estimatedRemoteDuration);
}
```

### On Task Stop (MainViewModel.cs:404-421)
```csharp
// 1. Send UI message
WeakReferenceMessenger.Default.Send(new TaskStartedMessage(""));
// 2. Stop local task
WorkSession.Stop();
// 3. Clear active_tracking in Supabase
await _syncService.StopTaskAsync();
```

### On App Close (MainViewModel.cs:366-389)
```csharp
// 1. Save to local XML
_dataService.SaveWorkSession(WorkSession);
// 2. Sync with Math.Max merge (pull → merge → push)
await SyncWithMergeAsync("Close");
// 3. Clear active tracking (user is no longer tracking anything)
await _syncService.StopTaskAsync();
```

## Testing Instructions

### Initial Setup
1. Create Supabase project at https://supabase.com
2. Go to SQL Editor → Run supabase-setup.sql
3. Go to API Settings → Copy URL and anon key
4. Copy `appsettings.local.json.template` to `appsettings.local.json`
5. Fill in your Supabase URL and anon key
6. Build and run the app

### Testing Phase 3: Active Task Lock

**Test Scenario 1: Conflict Detection**
1. PC1: Start tracking "Task A"
2. PC2: Start tracking "Task B" (or same task)
3. PC1: Within 30 seconds, should see MessageBox notification that PC2 took over
4. PC1: Local task should be automatically stopped
5. Verify: Only PC2's task is in active_tracking table

**Test Scenario 2: In-Flight Time Capture**
1. PC1: Start tracking "Task A", let it run for 60 seconds
2. PC2: Start tracking "Task A" (within 30s of PC1's last heartbeat)
3. PC2: Should capture PC1's in-flight time
4. PC2: Task should start from ~60 seconds + time_since_last_update
5. Verify: No time lost from PC1's tracking

**Test Scenario 3: Heartbeat and Sync**
1. PC1: Start tracking a task
2. Watch Debug output for "[Timer] Heartbeat sent" every 30 seconds
3. Check Supabase active_tracking table - updated_at should refresh every 30s
4. Verify: Task durations sync correctly via Math.Max merge

**Test Scenario 4: Cross-Timezone**
1. PC1: Set timezone to UTC+2
2. PC2: Set timezone to UTC+3
3. Run conflict detection test
4. Verify: No negative time differences, conflicts detected correctly

## Known Issues & Considerations

### Resolved Issues
1. ✅ Supabase version: Using 0.16.2 (0.17.0 doesn't exist yet)
2. ✅ Async/await in WPF: Moved auth to MainWindow.Loaded
3. ✅ API compatibility: Adjusted to Postgrest.Attributes for 0.16.2
4. ✅ Config file not copying: Added CopyToOutputDirectory in csproj
5. ✅ Guid.Empty IDs: Fixed by using fetch-then-insert-or-update pattern
6. ✅ Authentication timing: Added AuthenticationCompletedMessage to trigger sync after auth
7. ✅ SessionDate timezone: Fixed to use DateOnly.FromDateTime(DateTime.Today) instead of session.Today
8. ✅ Duplicate key violations: Added try-catch with fetch-on-failure for race conditions
9. ✅ Cross-timezone sync: Fixed timestamp comparisons using ToUniversalTime()
10. ✅ Negative time differences: Supabase returns timestamps in local time, convert to UTC for comparisons

### Current Limitations (Phase 3)
- ⚠️ **Not real-time yet**: Changes sync every 30s, not instant
- ⚠️ **No stale lock handling**: If app crashes, active_tracking not cleared (requires manual cleanup or timeout)
- ⚠️ **No offline queue**: Failed syncs are just logged to Debug
- ⚠️ **MessageBox notification**: Blocking dialog, could be improved with toast notification

### What Phase 4 Will Add
- Realtime subscriptions for instant updates (no polling)
- Offline queue for retry logic
- Sync status indicator in UI
- Better conflict resolution UI

## Important Code Patterns

### Error Handling
All sync operations wrapped in try-catch:
```csharp
if (_syncService != null)
{
    try
    {
        await _syncService.PushSessionAsync(WorkSession);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Failed to push: {ex.Message}");
    }
}
```

### Async void Event Handlers
Used for WPF event handlers that need async:
```csharp
private async void saveSessionTimer_Tick(object sender, EventArgs e)
private async void WorkSession_TaskStarted(string taskName)
private async void OnStop()
private async void OnClosing()
```

### Session Merging
```csharp
public static WorkSession MergeSessions(WorkSession local, WorkSession remote)
{
    var merged = new WorkSession { SessionDate = local.SessionDate };
    var allTaskNames = local.Tasks.Select(t => t.Name)
        .Union(remote.Tasks.Select(t => t.Name))
        .Distinct();

    foreach (var taskName in allTaskNames)
    {
        var localTask = local.Tasks.FirstOrDefault(t => t.Name == taskName);
        var remoteTask = remote.Tasks.FirstOrDefault(t => t.Name == taskName);

        var maxDuration = Math.Max(
            localTask?.DurationSeconds ?? 0,
            remoteTask?.DurationSeconds ?? 0
        );

        merged.AddTask(taskName);
        merged.Tasks.First(t => t.Name == taskName).DurationSeconds = maxDuration;
    }
    return merged;
}
```

## Git Branch
- Development branch: `claude/time-tracking-app-016DgiVw9Roh18q9QG3Te31G`
- Latest commits:
  - 143abd8: Integrate sync into MainViewModel
  - 5a07f50: Add SyncService for Supabase integration
  - 6c8f3c8: Clean up authentication flow and enable real Supabase auth

## Next Session Tasks

### Immediate Next Steps:
1. **Test Phase 3 with two physical devices** (PRIORITY)
   - Test conflict detection and auto-stop
   - Test in-flight time capture
   - Test cross-timezone scenarios
   - Verify heartbeat mechanism works correctly

2. **Phase 4: Realtime + Polish** (Future)
   - Implement Supabase Realtime subscriptions for instant updates
   - Add offline queue for failed sync operations
   - Add sync status indicator in UI
   - Replace MessageBox with toast notification
   - Implement stale lock cleanup (timeout after 5 minutes of no heartbeat)

### Questions to Consider:
- Do you want to add a sync status indicator showing connection state?
- Should we implement stale lock cleanup before Phase 4?
- Do you want toast notifications instead of MessageBox?
- Should we add a visual indicator showing which device is currently tracking?

## Useful Commands

```bash
# Build project
dotnet build

# Run app
dotnet run --project TrackMvvm/TrackMvvm.csproj

# Check git status
git status

# View Supabase tables
# Go to Supabase Dashboard → Table Editor

# Check Debug output in Visual Studio
# View → Output → Show output from: Debug
```

## Documentation Files
- `SUPABASE_SETUP.md` - Complete setup guide with screenshots
- `supabase-setup.sql` - Database schema and RLS policies
- `appsettings.local.json.template` - Config template
- `claude.md` - This file

---

**Last Updated**: 2025-12-25
**Phase**: 3 Complete, Phase 4 Next
**Status**: Ready for Phase 3 testing with two devices

**Recent Commits**:
- `413f99f`: Fix timezone handling in conflict detection and in-flight capture
- `d308b6e`: Fix SessionDate timezone issue by using DateOnly type
- `869e73c`: Handle duplicate key constraint violations in SyncSessionAsync
