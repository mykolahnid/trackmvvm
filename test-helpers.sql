-- Supabase SQL Helper Queries for Phase 2 Testing
-- Run these in Supabase Dashboard → SQL Editor to verify sync behavior

-- ============================================
-- 1. VIEW ALL YOUR DATA
-- ============================================

-- Show all work sessions for current user
SELECT
    id,
    session_date,
    created_at,
    updated_at,
    (updated_at - created_at) as time_since_creation
FROM work_sessions
WHERE user_id = auth.uid()
ORDER BY session_date DESC;

-- Show all tasks for today's session
SELECT
    ws.session_date,
    tt.task_name,
    tt.duration_seconds,
    (tt.duration_seconds / 60.0) as duration_minutes,
    tt.updated_at
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid()
  AND ws.session_date = CURRENT_DATE
ORDER BY tt.task_name;

-- Show current active tracking state
SELECT
    device_id,
    task_name,
    started_at,
    updated_at,
    (now() - updated_at) as time_since_last_update
FROM active_tracking
WHERE user_id = auth.uid();

-- ============================================
-- 2. VERIFY SYNC OPERATIONS
-- ============================================

-- Check if today's session exists (should exist after first sync)
SELECT
    CASE
        WHEN COUNT(*) > 0 THEN 'Session exists for today ✓'
        ELSE 'No session found for today ✗'
    END as status
FROM work_sessions
WHERE user_id = auth.uid()
  AND session_date = CURRENT_DATE;

-- Count tasks in today's session
SELECT
    COUNT(*) as task_count,
    SUM(duration_seconds) as total_seconds,
    SUM(duration_seconds) / 60.0 as total_minutes
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid()
  AND ws.session_date = CURRENT_DATE;

-- Check if active tracking is set
SELECT
    CASE
        WHEN task_name IS NOT NULL THEN 'Task is active: ' || task_name || ' on ' || device_id
        ELSE 'No active task'
    END as status,
    started_at,
    updated_at
FROM active_tracking
WHERE user_id = auth.uid();

-- ============================================
-- 3. MANUAL DATA INSERTION (for testing)
-- ============================================

-- Insert a test session for today (if doesn't exist)
-- Replace {your-device-name} with actual device name
INSERT INTO work_sessions (user_id, session_date, created_at, updated_at)
VALUES (
    auth.uid(),
    CURRENT_DATE,
    now(),
    now()
)
ON CONFLICT (user_id, session_date) DO NOTHING;

-- Insert test tasks into today's session
-- This simulates data from another device
WITH session AS (
    SELECT id FROM work_sessions
    WHERE user_id = auth.uid() AND session_date = CURRENT_DATE
)
INSERT INTO task_times (session_id, task_name, duration_seconds, updated_at)
SELECT
    session.id,
    'Remote Test Task',
    3600, -- 1 hour
    now()
FROM session
ON CONFLICT (session_id, task_name)
DO UPDATE SET
    duration_seconds = 3600,
    updated_at = now();

-- Simulate another device starting a task
-- Replace 'OTHER-PC' with a fake device name
INSERT INTO active_tracking (user_id, device_id, task_name, started_at, updated_at)
VALUES (
    auth.uid(),
    'OTHER-PC',
    'Task from other device',
    now(),
    now()
)
ON CONFLICT (user_id)
DO UPDATE SET
    device_id = 'OTHER-PC',
    task_name = 'Task from other device',
    started_at = now(),
    updated_at = now();

-- ============================================
-- 4. CLEANUP / RESET
-- ============================================

-- Clear active tracking (simulate stop)
UPDATE active_tracking
SET
    device_id = '',
    task_name = NULL,
    started_at = NULL,
    updated_at = now()
WHERE user_id = auth.uid();

-- Delete a specific task from today
DELETE FROM task_times
WHERE session_id IN (
    SELECT id FROM work_sessions
    WHERE user_id = auth.uid() AND session_date = CURRENT_DATE
)
AND task_name = 'Remote Test Task';

-- Delete ALL tasks from today (careful!)
DELETE FROM task_times
WHERE session_id IN (
    SELECT id FROM work_sessions
    WHERE user_id = auth.uid() AND session_date = CURRENT_DATE
);

-- Delete today's session entirely (cascades to tasks)
DELETE FROM work_sessions
WHERE user_id = auth.uid()
  AND session_date = CURRENT_DATE;

-- ============================================
-- 5. DEBUGGING QUERIES
-- ============================================

-- Find sessions with their task counts
SELECT
    ws.session_date,
    COUNT(tt.id) as task_count,
    SUM(tt.duration_seconds) / 60.0 as total_minutes,
    ws.updated_at as last_sync
FROM work_sessions ws
LEFT JOIN task_times tt ON tt.session_id = ws.id
WHERE ws.user_id = auth.uid()
GROUP BY ws.id, ws.session_date, ws.updated_at
ORDER BY ws.session_date DESC
LIMIT 10;

-- Find duplicate tasks (should be 0)
SELECT
    ws.session_date,
    tt.task_name,
    COUNT(*) as duplicate_count
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid()
GROUP BY ws.session_date, tt.task_name
HAVING COUNT(*) > 1;

-- Check for stale active tracking (no update in 5+ minutes)
SELECT
    device_id,
    task_name,
    started_at,
    updated_at,
    EXTRACT(EPOCH FROM (now() - updated_at)) / 60 as minutes_stale
FROM active_tracking
WHERE user_id = auth.uid()
  AND task_name IS NOT NULL
  AND (now() - updated_at) > INTERVAL '5 minutes';

-- View recent activity (last 7 days)
SELECT
    ws.session_date,
    tt.task_name,
    tt.duration_seconds / 3600.0 as hours,
    tt.updated_at
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid()
  AND ws.session_date > CURRENT_DATE - INTERVAL '7 days'
ORDER BY ws.session_date DESC, tt.task_name;

-- ============================================
-- 6. TEST SCENARIOS
-- ============================================

-- Scenario: Simulate merge conflict (local = 100s, remote = 200s)
-- Expected: App should use Math.Max = 200s
WITH session AS (
    SELECT id FROM work_sessions
    WHERE user_id = auth.uid() AND session_date = CURRENT_DATE
)
INSERT INTO task_times (session_id, task_name, duration_seconds, updated_at)
SELECT id, 'MergeTest', 200, now()
FROM session
ON CONFLICT (session_id, task_name)
DO UPDATE SET duration_seconds = 200, updated_at = now();
-- Now in app, manually set same task to 100 seconds and restart
-- Verify app shows 200 seconds after pull

-- Scenario: Check if push timer works (wait 60+ seconds after starting task)
-- Run this query immediately after 60 second timer fires
SELECT
    task_name,
    duration_seconds,
    updated_at,
    EXTRACT(EPOCH FROM (now() - updated_at)) as seconds_ago
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid()
  AND ws.session_date = CURRENT_DATE
ORDER BY updated_at DESC;
-- updated_at should be within last few seconds

-- ============================================
-- 7. QUICK STATUS CHECK
-- ============================================

-- Run this to get a complete overview
SELECT
    'Sessions' as type,
    COUNT(*)::text as count,
    MAX(session_date)::text as latest
FROM work_sessions
WHERE user_id = auth.uid()

UNION ALL

SELECT
    'Tasks Today' as type,
    COUNT(*)::text as count,
    SUM(duration_seconds / 60)::text || ' min' as latest
FROM task_times tt
JOIN work_sessions ws ON ws.id = tt.session_id
WHERE ws.user_id = auth.uid() AND ws.session_date = CURRENT_DATE

UNION ALL

SELECT
    'Active Tracking' as type,
    COALESCE(task_name, 'None')::text as count,
    COALESCE(device_id, 'N/A')::text as latest
FROM active_tracking
WHERE user_id = auth.uid();
