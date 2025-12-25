# Phase 2 Testing - Quick Start Guide

## Pre-Flight Checklist

Before testing, ensure you have:

- [x] **Build succeeds**: Project compiles without errors ✓
- [ ] **Supabase project created**: Free tier account at supabase.com
- [ ] **Database schema deployed**: Ran `supabase-setup.sql` in SQL Editor
- [ ] **User account created**: In Authentication → Users
- [ ] **Config file ready**: `appsettings.local.json` with URL and AnonKey
- [ ] **Login credentials**: Email/password from Supabase

---

## 🚀 Quick Test (5 minutes)

This test verifies all Phase 2 features are working:

### Step 1: Launch & Login (1 min)
```bash
cd C:\apps\trackmvvm
dotnet run --project TrackMvvm\TrackMvvm.csproj
```

1. Enter your Supabase email/password
2. Check "Remember Me"
3. Click Login
4. ✅ Main window should appear

### Step 2: Create & Track Task (1 min)
1. Click "Add Task"
2. Enter: `Phase2Test`
3. Click the task to start tracking
4. Wait 10 seconds
5. Click "Stop"

### Step 3: Verify in Supabase (2 min)
Go to Supabase Dashboard → Table Editor:

**Check `work_sessions`**:
- Should have 1 row with today's date
- `user_id` matches your user
- `updated_at` is recent

**Check `task_times`**:
- Should have 1 row: "Phase2Test"
- `duration_seconds` ≈ 10
- Linked to today's session

**Check `active_tracking`**:
- `task_name` = NULL (since you stopped)
- `device_id` = "" (empty)

### Step 4: Test Pull on Restart (1 min)
1. Close app
2. Relaunch app
3. ✅ "Phase2Test" should appear with ~10 seconds duration
4. ✅ Debug Output shows: `Successfully pulled and merged remote session`

---

## 🔬 Comprehensive Test (20 minutes)

Follow the detailed test plan in [PHASE_2_TEST_PLAN.md](PHASE_2_TEST_PLAN.md):

- Test 1: Authentication ✓ (already done in Quick Test)
- Test 2: Pull on Startup
- Test 3: Push Timer (60 seconds)
- Test 4: Push on Close
- Test 5: Last-Writer-Wins
- Test 6: Stop Tracking
- Test 7: Merge Logic
- Test 8: Multi-Device Simulation

---

## 🛠️ Debug Output Monitoring

**How to view Debug Output**:
- Visual Studio: Menu → View → Output → Select "Debug" dropdown
- VS Code: Debug Console when running with debugger
- Rider: Run → View → Show Console

**What to look for**:
```
✅ [SupabaseService] Instance created
✅ [SupabaseService] Authentication successful. CurrentUser: {uuid}
✅ [SyncService] PullSessionAsync called. IsAuthenticated: True
✅ [SupabaseService] GetTodaySessionAsync: Found session {guid}
✅ Successfully pulled and merged remote session after authentication
✅ [SupabaseService] SyncSessionAsync: Found existing session {guid}
```

**Red flags** (investigate if you see):
```
❌ Failed to pull remote session: {error}
❌ Failed to push session: {error}
❌ Authentication failed
❌ Not authenticated
```

---

## 📊 SQL Verification Queries

Use the queries in [test-helpers.sql](test-helpers.sql) to inspect database state.

**Most useful queries**:

### Quick Status Check
```sql
-- Run in Supabase SQL Editor
SELECT 'Sessions' as type, COUNT(*)::text as count
FROM work_sessions WHERE user_id = auth.uid()
UNION ALL
SELECT 'Tasks Today', COUNT(*)::text
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid() AND ws.session_date = CURRENT_DATE;
```

### View Today's Data
```sql
SELECT tt.task_name, tt.duration_seconds, tt.updated_at
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid() AND ws.session_date = CURRENT_DATE
ORDER BY tt.task_name;
```

### Check Active Tracking
```sql
SELECT device_id, task_name, started_at, updated_at
FROM active_tracking WHERE user_id = auth.uid();
```

---

## 🎯 Test Scenarios

### Scenario A: Fresh Start (No Data)
**Setup**: Delete all your data in Supabase
**Test**: Launch app, add task, track for 30s, close
**Expected**: New session + task created in Supabase

### Scenario B: Remote Data Exists
**Setup**: Manually insert task in Supabase with 3600 seconds
**Test**: Launch app
**Expected**: Task appears with 1 hour (3600s)

### Scenario C: Merge Conflict
**Setup**:
1. Add task "ConflictTest" locally, track for 100 seconds
2. Manually update in Supabase to 200 seconds
3. Restart app
**Expected**: Shows 200 seconds (Math.Max wins)

### Scenario D: Timer Test
**Setup**: Launch app, add task, start tracking
**Test**: Wait 65 seconds without stopping
**Expected**:
- At ~60s, Debug Output shows sync
- Supabase shows updated duration
- No app restart needed

---

## ⚠️ Known Phase 2 Limitations

These are **expected** and will be fixed in Phase 3/4:

✅ **Works as designed**:
- Data syncs on startup (pull)
- Data syncs every 60s (push)
- Data syncs on close (push)
- Last-writer-wins on task start
- Math.Max merge for durations

❌ **Not implemented yet**:
- Real-time updates (must restart to see other device changes)
- Heartbeat (no periodic active_tracking updates)
- Auto-stop if another device starts task
- Stale lock cleanup
- Offline queue for failed syncs

---

## 🐛 Troubleshooting

### Problem: Login fails with "Authentication error"
**Solutions**:
- Verify user exists in Supabase → Authentication → Users
- Check "Auto Confirm User" was enabled when creating user
- Try resetting password in Supabase dashboard
- Verify appsettings.local.json has correct URL and key

### Problem: "Not authenticated" in Debug Output
**Solutions**:
- Delete Windows Credential: Win+R → `control /name Microsoft.CredentialManager` → Remove "TrackMvvm:Supabase"
- Restart app and login again
- Check SupabaseService shows authentication success

### Problem: No data appears after restart
**Solutions**:
- Check Debug Output for pull messages
- Verify Supabase has data in table editor
- Check RLS policies are applied (run supabase-setup.sql again)
- Ensure session_date matches today's date

### Problem: Data not pushing to Supabase
**Solutions**:
- Wait full 60 seconds for timer
- Check Debug Output for "SyncSessionAsync" messages
- Verify internet connection
- Check Supabase → Logs for errors

### Problem: Timer not firing
**Solutions**:
- Verify code in MainViewModel.cs:76 shows `TimeSpan.FromMinutes(1)`
- Check timer started: MainViewModel.cs:77 `saveSessionTimer.Start()`
- Ensure app stays open for 60+ seconds

---

## ✅ Success Criteria

Phase 2 testing is successful when:

- [ ] Login works with credential storage
- [ ] App pulls remote data on startup
- [ ] App pushes data every 60 seconds
- [ ] App pushes data on close
- [ ] Starting task updates active_tracking
- [ ] Stopping task clears active_tracking
- [ ] Merge uses Math.Max correctly
- [ ] No data loss in any scenario
- [ ] Debug Output shows all expected messages

---

## 📝 Test Results Template

Copy this to track your testing:

```
Date: _______________
Tester: _______________
Build Version: _______________

QUICK TEST (5 min):
[ ] Launch & Login - Pass/Fail: _____
[ ] Create & Track Task - Pass/Fail: _____
[ ] Verify in Supabase - Pass/Fail: _____
[ ] Pull on Restart - Pass/Fail: _____

COMPREHENSIVE TEST (20 min):
[ ] Test 1: Authentication - Pass/Fail: _____
[ ] Test 2: Pull on Startup - Pass/Fail: _____
[ ] Test 3: Push Timer - Pass/Fail: _____
[ ] Test 4: Push on Close - Pass/Fail: _____
[ ] Test 5: Last-Writer-Wins - Pass/Fail: _____
[ ] Test 6: Stop Tracking - Pass/Fail: _____
[ ] Test 7: Merge Logic - Pass/Fail: _____
[ ] Test 8: Multi-Device Sim - Pass/Fail: _____

OVERALL: [ ] PASS [ ] FAIL

Notes:
_________________________________
_________________________________
_________________________________
```

---

## 🎉 Next Steps

**If all tests pass**:
- Mark Phase 2 as complete ✓
- Review Phase 3 requirements
- Plan implementation of heartbeat + periodic pull
- Consider notifications for remote task changes

**If tests fail**:
- Document specific failure
- Capture Debug Output
- Check Supabase logs
- Review code at failure point
- Ask for help with specific error messages

---

## 🔗 Related Files

- [PHASE_2_TEST_PLAN.md](PHASE_2_TEST_PLAN.md) - Detailed test procedures
- [test-helpers.sql](test-helpers.sql) - SQL queries for verification
- [SUPABASE_SETUP.md](SUPABASE_SETUP.md) - Initial setup guide
- [CLAUDE.md](CLAUDE.md) - Complete project documentation

Good luck with testing! 🚀
