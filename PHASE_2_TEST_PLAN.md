# Phase 2 Sync Testing Plan

## Test Environment Setup

### Prerequisites
- [x] Supabase project created and configured
- [x] Database schema deployed (supabase-setup.sql)
- [x] appsettings.local.json configured with URL and AnonKey
- [ ] User account created in Supabase
- [ ] App builds successfully

### Configuration Checklist
1. Verify `appsettings.local.json` exists in TrackMvvm folder
2. Check file contains valid Supabase URL and AnonKey
3. Ensure .gitignore prevents committing config file
4. Build project: `dotnet build`

---

## Test Suite

### Test 1: Authentication Flow ✅

**Goal**: Verify login process and credential storage

**Steps**:
1. Launch the app
2. Enter email and password from Supabase user account
3. Check "Remember Me" checkbox
4. Click "Login"

**Expected Results**:
- ✅ Login dialog appears on first launch
- ✅ Authentication succeeds with valid credentials
- ✅ MainWindow opens after successful login
- ✅ Credentials stored in Windows Credential Manager (Control Panel → Credential Manager → Windows Credentials → Look for "TrackMvvm:Supabase")

**How to Verify**:
- Check Debug Output window for: `[SupabaseService] Authentication successful`
- Restart app - should auto-login without showing dialog

**Failure Scenarios to Test**:
- [ ] Wrong password → Should show error message
- [ ] Empty fields → Should show validation error
- [ ] Cancel login → App should close

---

### Test 2: Pull on Startup (Remote Data Merge) 📥

**Goal**: Verify app downloads and merges remote data on startup

**Setup**:
1. Use Supabase Dashboard → Table Editor
2. Manually insert test data:
   - In `work_sessions`: Add row with today's date and your user_id
   - In `task_times`: Add 2-3 tasks with durations (e.g., "Remote Task 1" = 3600 seconds)
3. Launch TrackMvvm app

**Steps**:
1. Close app if running
2. Launch app (auto-login should occur)
3. Wait for main window to appear

**Expected Results**:
- ✅ App loads remote tasks from Supabase
- ✅ Tasks appear in UI with correct durations
- ✅ Debug output shows: `Successfully pulled and merged remote session after authentication`
- ✅ If local XML has same task with different duration, Math.Max wins

**How to Verify**:
```
Debug Output should show:
[SyncService] PullSessionAsync called. IsAuthenticated: True
[SupabaseService] GetTodaySessionAsync: Found session {guid}
[SupabaseService] GetTodaySessionAsync: Found {N} tasks
Successfully pulled and merged remote session after authentication
```

**Test Variations**:
- [ ] Empty remote database → Local tasks remain
- [ ] Remote has tasks local doesn't → Remote tasks added
- [ ] Local has tasks remote doesn't → Local tasks kept
- [ ] Both have same task with different durations → Larger duration wins

---

### Test 3: Push on Save Timer (Every 60 seconds) 📤

**Goal**: Verify automatic push to Supabase every minute

**Steps**:
1. Launch app
2. Add a new task: "Test Task Timer"
3. Start tracking the task
4. Wait 60+ seconds (check timer interval in code: 60s)
5. Check Supabase Table Editor

**Expected Results**:
- ✅ After 60 seconds, data pushes to Supabase
- ✅ Debug output shows: `[SupabaseService] SyncSessionAsync called`
- ✅ In Supabase → work_sessions table: Row exists for today
- ✅ In Supabase → task_times table: "Test Task Timer" exists with accumulated duration
- ✅ updated_at timestamp is recent

**How to Verify**:
```
Watch Debug Output at ~60s mark:
[SupabaseService] SyncSessionAsync: Found existing session {guid}
OR
[SupabaseService] SyncSessionAsync: Inserted new session {guid}
```

**In Supabase Dashboard**:
- Go to Table Editor → work_sessions → Verify row exists for today
- Go to Table Editor → task_times → Verify task exists with correct duration_seconds

---

### Test 4: Push on Close 📤

**Goal**: Verify data syncs when app closes

**Steps**:
1. Launch app
2. Add task: "Close Test Task"
3. Track for 30 seconds (less than timer interval)
4. Click X to close app (or use Close command)
5. Check Supabase immediately

**Expected Results**:
- ✅ Data pushed before app closes
- ✅ Debug output shows: `[SupabaseService] SyncSessionAsync called` before shutdown
- ✅ Supabase has latest data including the 30-second task

**How to Verify**:
- Relaunch app → Task duration matches what you had before closing
- Check Supabase Table Editor → Verify timestamp is from app close time

---

### Test 5: Last-Writer-Wins on Task Start 🏆

**Goal**: Verify active_tracking table updates when starting a task

**Steps**:
1. Launch app
2. Add task: "Active Task Test"
3. Click task to start tracking
4. Immediately check Supabase Table Editor → active_tracking table

**Expected Results**:
- ✅ Debug output shows: `StartTaskTrackingAsync called`
- ✅ In active_tracking table:
  - user_id = your user UUID
  - device_id = your computer name (Environment.MachineName)
  - task_name = "Active Task Test"
  - started_at = current timestamp
  - updated_at = current timestamp

**How to Verify**:
```sql
-- In Supabase SQL Editor:
SELECT * FROM active_tracking WHERE user_id = '{your-user-id}';
```

Should return 1 row with correct data.

---

### Test 6: Stop Tracking 🛑

**Goal**: Verify active_tracking clears when stopping a task

**Steps**:
1. Launch app
2. Start tracking any task
3. Click "Stop" button
4. Check Supabase → active_tracking table

**Expected Results**:
- ✅ Debug output shows: `StopTaskTrackingAsync called`
- ✅ In active_tracking table:
  - task_name = NULL (or empty)
  - device_id = "" (empty)
  - started_at = NULL

**How to Verify**:
```sql
SELECT task_name, device_id, started_at
FROM active_tracking
WHERE user_id = '{your-user-id}';
```

All values should be NULL/empty.

---

### Test 7: Session Merging Logic 🔀

**Goal**: Test Math.Max merge strategy with conflicting data

**Setup**:
1. Add task "Merge Test" locally, track for 100 seconds
2. In Supabase, manually update same task to 200 seconds
3. Restart app

**Expected Results**:
- ✅ App uses 200 seconds (Math.Max wins)
- ✅ Debug shows merge happened

**Reverse Test**:
1. Local has 300 seconds
2. Remote has 100 seconds
3. Restart app
4. ✅ Should keep 300 seconds

---

### Test 8: Multi-Device Simulation 🖥️🖥️

**Goal**: Simulate two devices syncing (manual simulation)

**Steps**:
1. **Device 1 (Simulated)**:
   - Use Supabase Dashboard to manually insert data
   - In work_sessions: Add today's session
   - In task_times: Add "Device1 Task" with 1800 seconds
   - In active_tracking: Set device_id="DEVICE1", task_name="Device1 Task"

2. **Device 2 (Your App)**:
   - Launch app
   - Pull should bring in "Device1 Task"
   - Start tracking "Device2 Task"
   - Check active_tracking updates to your device

**Expected Results**:
- ✅ "Device1 Task" appears in your app with 1800 seconds
- ✅ When you start "Device2 Task", active_tracking shows your device_id
- ✅ Both tasks' durations are preserved

---

## Debugging Tips

### Enable Verbose Logging
All sync operations write to Debug Output. To view:
- Visual Studio: View → Output → Show output from: Debug
- VS Code: Debug Console when debugging

### Key Debug Messages to Look For:
```
✅ [SupabaseService] Instance created
✅ [SupabaseService] Authentication successful
✅ [SyncService] PullSessionAsync called
✅ Successfully pulled and merged remote session
✅ [SupabaseService] SyncSessionAsync: Found existing session
✅ Failed to push session: {error}  ← Investigate if this appears
```

### Common Issues:

**"Not authenticated"**
- Check credentials in Credential Manager
- Try logging out and back in
- Verify user exists in Supabase dashboard

**"No session found for today"**
- Expected on first run (will create new session)
- Check Supabase dashboard date matches local date

**"Insert failed (duplicate key)"**
- Normal - code handles this with fetch-then-update pattern
- Should see: "Found session after insert failure"

**Timer not firing**
- Check MainViewModel.cs line 76: `saveSessionTimer.Interval = TimeSpan.FromMinutes(1)`
- Verify timer started: `saveSessionTimer.Start()`

---

## Test Completion Checklist

### Phase 2 Feature Verification:
- [ ] Authentication works (login dialog, credential storage)
- [ ] Pull on startup merges remote data
- [ ] Push happens every 60 seconds
- [ ] Push happens on app close
- [ ] Starting task updates active_tracking
- [ ] Stopping task clears active_tracking
- [ ] Math.Max merge strategy works correctly
- [ ] No data loss in any scenario

### Known Phase 2 Limitations (Expected):
- ⚠️ No real-time updates (must restart to see other device's changes)
- ⚠️ No heartbeat (active_tracking updated_at only on start/stop)
- ⚠️ No auto-stop if another device starts task (Phase 3)
- ⚠️ No stale lock cleanup (Phase 3)
- ⚠️ No offline queue (Phase 4)

---

## Next Steps

### If All Tests Pass ✅:
Ready to proceed to **Phase 3: Active Task Lock**
- Add heartbeat timer (30s)
- Add pull timer (60s)
- Implement auto-stop logic
- Add notifications

### If Tests Fail ❌:
1. Note which test failed
2. Check Debug Output for error messages
3. Verify Supabase dashboard shows expected data
4. Check RLS policies allow your operations
5. Report findings for debugging

---

## Test Results Log

Date: _______________
Tester: _______________

| Test # | Test Name | Status | Notes |
|--------|-----------|--------|-------|
| 1 | Authentication | ⬜ Pass / ⬜ Fail | |
| 2 | Pull on Startup | ⬜ Pass / ⬜ Fail | |
| 3 | Push Timer | ⬜ Pass / ⬜ Fail | |
| 4 | Push on Close | ⬜ Pass / ⬜ Fail | |
| 5 | Last-Writer-Wins Start | ⬜ Pass / ⬜ Fail | |
| 6 | Stop Tracking | ⬜ Pass / ⬜ Fail | |
| 7 | Merge Logic | ⬜ Pass / ⬜ Fail | |
| 8 | Multi-Device Sim | ⬜ Pass / ⬜ Fail | |

**Overall Phase 2 Status**: ⬜ Ready for Phase 3 / ⬜ Needs fixes
