-- TrackMvvm Supabase Database Setup Script
-- Run this in your Supabase SQL Editor

-- ============================================
-- 1. CREATE TABLES
-- ============================================

-- Work Sessions Table (one per day per user)
CREATE TABLE IF NOT EXISTS work_sessions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    session_date DATE NOT NULL,
    created_at TIMESTAMPTZ DEFAULT now(),
    updated_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE(user_id, session_date)
);

-- Task Times Table (tracks time for each task in a session)
CREATE TABLE IF NOT EXISTS task_times (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    session_id UUID NOT NULL REFERENCES work_sessions(id) ON DELETE CASCADE,
    task_name TEXT NOT NULL,
    duration_seconds NUMERIC(10,2) NOT NULL DEFAULT 0,
    updated_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE(session_id, task_name)
);

-- Active Tracking Table (global state - which device is tracking what)
CREATE TABLE IF NOT EXISTS active_tracking (
    user_id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    active_task_id UUID REFERENCES task_times(id) ON DELETE SET NULL,
    device_id TEXT NOT NULL DEFAULT '',
    task_name TEXT,
    started_at TIMESTAMPTZ,
    updated_at TIMESTAMPTZ DEFAULT now()
);

-- ============================================
-- 2. CREATE INDEXES
-- ============================================

CREATE INDEX IF NOT EXISTS idx_work_sessions_date ON work_sessions(session_date);
CREATE INDEX IF NOT EXISTS idx_work_sessions_user ON work_sessions(user_id);
CREATE INDEX IF NOT EXISTS idx_task_times_session ON task_times(session_id);

-- ============================================
-- 3. ENABLE ROW LEVEL SECURITY
-- ============================================

ALTER TABLE work_sessions ENABLE ROW LEVEL SECURITY;
ALTER TABLE task_times ENABLE ROW LEVEL SECURITY;
ALTER TABLE active_tracking ENABLE ROW LEVEL SECURITY;

-- ============================================
-- 4. CREATE RLS POLICIES
-- ============================================

-- Work Sessions: Users can only access their own sessions
DROP POLICY IF EXISTS "Users manage own sessions" ON work_sessions;
CREATE POLICY "Users manage own sessions" ON work_sessions
    FOR ALL
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

-- Task Times: Users can only access tasks in their own sessions
DROP POLICY IF EXISTS "Users manage own tasks" ON task_times;
CREATE POLICY "Users manage own tasks" ON task_times
    FOR ALL
    USING (
        session_id IN (
            SELECT id FROM work_sessions WHERE user_id = auth.uid()
        )
    )
    WITH CHECK (
        session_id IN (
            SELECT id FROM work_sessions WHERE user_id = auth.uid()
        )
    );

-- Active Tracking: Users can only access their own tracking state
DROP POLICY IF EXISTS "Users manage own active state" ON active_tracking;
CREATE POLICY "Users manage own active state" ON active_tracking
    FOR ALL
    USING (auth.uid() = user_id)
    WITH CHECK (auth.uid() = user_id);

-- ============================================
-- 5. CREATE UPDATED_AT TRIGGER
-- ============================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = now();
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

-- Apply trigger to all tables
DROP TRIGGER IF EXISTS update_work_sessions_updated_at ON work_sessions;
CREATE TRIGGER update_work_sessions_updated_at
    BEFORE UPDATE ON work_sessions
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_task_times_updated_at ON task_times;
CREATE TRIGGER update_task_times_updated_at
    BEFORE UPDATE ON task_times
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

DROP TRIGGER IF EXISTS update_active_tracking_updated_at ON active_tracking;
CREATE TRIGGER update_active_tracking_updated_at
    BEFORE UPDATE ON active_tracking
    FOR EACH ROW
    EXECUTE FUNCTION update_updated_at_column();

-- ============================================
-- 6. ENABLE REALTIME (for live sync)
-- ============================================

-- Enable Realtime for active_tracking table
ALTER PUBLICATION supabase_realtime ADD TABLE active_tracking;

-- ============================================
-- SETUP COMPLETE!
-- ============================================

-- Next steps:
-- 1. In Supabase Dashboard, go to Authentication > Providers > Email
-- 2. UNCHECK "Enable email signups" (to prevent others from creating accounts)
-- 3. Go to Authentication > Users
-- 4. Click "Add user" and create your account
-- 5. Copy your email - you'll use this in the app
-- 6. Go to Settings > API
-- 7. Copy "Project URL" and "anon public" key
-- 8. Create appsettings.local.json in your app with these values
