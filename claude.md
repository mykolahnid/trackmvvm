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

## Current Status: Phase 2 Complete ✅

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
- ✅ Push on save: Every minute (timer) and on app close
- ✅ Last-writer-wins on task start: Updates active_tracking table
- ✅ Stop tracking: Clears active_tracking when task stopped
- ✅ Session merging: Uses Math.Max for duration conflicts

### Phase 3: Active Task Lock 🔄 (Next)
**Goal**: Enforce last-writer-wins in real-time
- ⏳ Add periodic heartbeat (every 30s) to update active_tracking.updated_at
- ⏳ Add periodic pull (every 60s) to check for active task changes
- ⏳ Detect if another device started a task and auto-stop local task
- ⏳ Show notification when task is stopped by another device
- ⏳ Handle stale locks (no heartbeat for 5+ minutes)

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
  - Line 52-67: Pull and merge on startup
  - Line 81-97: Push on save (timer tick)
  - Line 99-115: Last-writer-wins on task start
  - Line 122-138: Push on close
  - Line 153-170: Stop tracking on task stop

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

### On Startup (MainViewModel.cs:52-67)
```csharp
// 1. Load local WorkSession from XML
// 2. Pull remote session from Supabase
var remoteSession = await _syncService.PullSessionAsync();
// 3. Merge using Math.Max for durations
WorkSession = SyncService.MergeSessions(WorkSession, remoteSession);
// 4. Update UI with merged tasks
```

### On Save (Every 60s + On Close)
```csharp
// 1. Save to local XML
_dataService.SaveWorkSession(WorkSession);
// 2. Push to Supabase
await _syncService.PushSessionAsync(WorkSession);
```

### On Task Start (Last-Writer-Wins)
```csharp
// 1. Send UI message
WeakReferenceMessenger.Default.Send(new TaskStartedMessage(taskName));
// 2. Update active_tracking in Supabase (upsert)
await _syncService.StartTaskAsync(taskName);
// Device ID and timestamp recorded for last-writer-wins
```

### On Task Stop
```csharp
// 1. Stop local task
WorkSession.Stop();
// 2. Clear active_tracking in Supabase
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

### Testing Sync Between Two "Devices"

**Method 1: Two Physical PCs**
1. Copy appsettings.local.json to both PCs
2. Sign in with same Supabase credentials on both
3. Start task on PC1 → Wait 60s → Check it appears on PC2 after restart
4. Start different task on PC2 → Check active_tracking table updated

**Method 2: Two App Instances (Same PC)**
- Run two instances of the app (if allowed by app design)
- Use different device IDs if needed for testing

**Method 3: Manual Database Testing**
1. Run app on PC1, start task "Task A"
2. Check Supabase dashboard → active_tracking table
3. Verify user_id, device_id, task_name, started_at populated
4. Manually insert record for different device_id
5. Restart app → Should see merged data

## Known Issues & Considerations

### Resolved Issues
1. ✅ Supabase version: Using 0.16.2 (0.17.0 doesn't exist yet)
2. ✅ Async/await in WPF: Moved auth to MainWindow.Loaded
3. ✅ API compatibility: Adjusted to Postgrest.Attributes for 0.16.2
4. ✅ Config file not copying: Added CopyToOutputDirectory in csproj
5. ✅ Guid.Empty IDs: Fixed by using fetch-then-insert-or-update pattern
6. ✅ Authentication timing: Added AuthenticationCompletedMessage to trigger sync after auth

### Current Limitations (Phase 2)
- ⚠️ **Not real-time yet**: Changes only sync on app restart or timer tick
- ⚠️ **No heartbeat**: Can't detect if another device's task is still active
- ⚠️ **No stale lock handling**: If app crashes, active_tracking not cleared
- ⚠️ **No offline queue**: Failed syncs are just logged to Debug

### What Phase 3 Will Fix
- Add heartbeat timer (30s) to keep active_tracking fresh
- Add pull timer (60s) to detect remote changes
- Auto-stop local task if remote device started newer task
- Clean up stale locks (no heartbeat for 5+ minutes)

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

### Immediate Next Steps (Phase 3):
1. Add heartbeat timer to MainViewModel (30s interval)
2. Add pull timer to MainViewModel (60s interval)
3. Implement auto-stop logic when remote device takes over
4. Add UI notification for task stopped by remote device
5. Implement stale lock cleanup in SupabaseService
6. Test two-device scenario thoroughly

### Questions to Ask User:
- Do you want to test Phase 2 first before proceeding to Phase 3?
- What kind of notification do you want when remote device stops your task? (MessageBox, toast, status bar?)
- Do you want a visual indicator showing which device is currently tracking?

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

**Last Updated**: 2025-11-24
**Phase**: 2 Complete, Phase 3 Next
**Status**: Ready for testing or Phase 3 implementation
