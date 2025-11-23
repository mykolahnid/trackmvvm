# Supabase Sync Setup Guide

This guide will help you set up Supabase synchronization for TrackMvvm so you can sync time tracking between multiple PCs.

## 📋 Prerequisites

- A Supabase account (free tier is sufficient)
- Two PCs where you want to use the app

---

## 🚀 Step 1: Create Supabase Project

1. Go to https://supabase.com
2. Sign up or log in
3. Click **"New Project"**
4. Fill in:
   - **Name**: `trackmvvm` (or any name you prefer)
   - **Database Password**: Choose a strong password (you won't need this often)
   - **Region**: Choose closest to you
5. Click **"Create new project"**
6. Wait 2-3 minutes for provisioning

---

## 🗄️ Step 2: Set Up Database

1. In your Supabase project dashboard, click **"SQL Editor"** in the left sidebar
2. Click **"New query"**
3. Open the file `supabase-setup.sql` from your TrackMvvm project folder
4. Copy the entire contents
5. Paste into the Supabase SQL Editor
6. Click **"Run"** (or press `Ctrl+Enter`)
7. You should see: "Success. No rows returned"

---

## 🔐 Step 3: Configure Authentication

### Disable Public Signups

1. In Supabase dashboard, go to **Authentication** → **Providers**
2. Find **Email** provider
3. **UNCHECK** the box: **"Enable email signups"**
4. Click **"Save"**

This prevents anyone else from creating accounts in your database.

### Create Your Account

1. Go to **Authentication** → **Users**
2. Click **"Add user"** → **"Create new user"**
3. Enter:
   - **Email**: Your email address
   - **Password**: Choose a password (you'll use this to login to the app)
   - Auto Confirm User: **✅ Checked**
4. Click **"Create user"**

---

## 🔑 Step 4: Get API Keys

1. In Supabase dashboard, go to **Settings** (gear icon at bottom left)
2. Click **"API"**
3. Find these two values:
   - **Project URL** (e.g., `https://xxxxx.supabase.co`)
   - **anon public** key (long string starting with `eyJ...`)

---

## ⚙️ Step 5: Configure TrackMvvm (PC #1)

1. In your TrackMvvm project folder, find `appsettings.local.json.template`
2. Copy it and rename to `appsettings.local.json` (remove `.template`)
3. Open `appsettings.local.json` in a text editor
4. Replace the values:

```json
{
  "Supabase": {
    "Url": "https://your-project-id.supabase.co",
    "AnonKey": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
}
```

5. **Important**: This file is gitignored and will NOT be committed to GitHub (secure)
6. Build and run the app
7. You'll see a login dialog
8. Enter the email/password you created in Step 3
9. Check **"Remember me"** (stores credentials securely in Windows Credential Manager)
10. Click **"Login"**

---

## 💻 Step 6: Configure TrackMvvm (PC #2)

1. Pull your latest code from GitHub (the app code is there, but not the keys)
2. Repeat **Step 5** on PC #2:
   - Copy `appsettings.local.json.template` → `appsettings.local.json`
   - Paste the same URL and AnonKey from Supabase
3. Run the app
4. Login with the same email/password

---

## ✅ Testing Sync

### Test Last-Writer-Wins

1. **PC #1**: Start tracking "ProjectA"
2. **PC #2**: The app should show "ProjectA" is active (give it a few seconds)
3. **PC #2**: Start tracking "ProjectB"
4. **PC #1**: Should auto-stop and show notification: "Tracking stopped - ProjectB started on PC2"

### Test Offline Sync

1. Disconnect PC #1 from internet
2. PC #1: Track "ProjectC" for 10 minutes
3. Reconnect internet
4. PC #1: Data syncs automatically
5. PC #2: Pull data (wait ~60 seconds or restart app)
6. PC #2: Should show "ProjectC" with 10 minutes

---

## 🎛️ How It Works

### Sync Behavior

| Event | What Happens |
|-------|--------------|
| **Start tracking a task** | Immediately writes to Supabase, takes over from other devices |
| **Stop tracking** | Immediately clears global lock in Supabase |
| **Every 30 seconds** | Pushes duration updates + heartbeat |
| **Every 60 seconds** | Pulls changes from other devices |
| **App startup** | Full sync (downloads today's session) |

### Last Writer Wins

- If PC #1 is tracking "ProjectA" and PC #2 starts "ProjectB"
- PC #2 wins automatically (no blocking)
- PC #1 receives Realtime notification and auto-stops
- Both PCs' accumulated time is preserved

### Offline Mode

- App works fully offline (file-based storage continues)
- When network returns, changes sync automatically
- Latest duration wins (no data loss)

---

## 🛠️ Troubleshooting

### Login fails

- Check email/password are correct
- Verify you created the user in Supabase dashboard
- Make sure "Auto Confirm User" was checked

### Sync not working

- Verify `appsettings.local.json` exists and has correct URL/key
- Check internet connection
- Look at Supabase dashboard → Logs for errors

### "Another device is tracking" but no device is

- Old tracking lock stuck (this shouldn't happen with current design)
- In Supabase dashboard, go to **Table Editor** → **active_tracking**
- Delete the row or set `task_name` to NULL

### Want to logout

- Delete credentials: **Windows** → Search "Credential Manager" → Find "TrackMvvm:Supabase" → Remove
- Next app launch will show login dialog again

---

## 🔒 Security Notes

### What's Safe to Commit to Git

✅ **Safe**: Supabase URL and anon key are in `appsettings.local.json` (gitignored)
✅ **Safe**: App code
❌ **Never commit**: `appsettings.local.json` (already in .gitignore)
❌ **Never commit**: Your email/password (stored in Windows Credential Manager only)

### Why It's Secure

- Anon key is designed to be public (used in web apps)
- Security enforced by Row Level Security (RLS) policies
- Users can only see/modify their own data
- Public signups disabled = only you can access
- Even if someone had your URL + anon key, they can't create accounts

---

## 📊 Free Tier Limits

Your usage vs Supabase free tier:

| Resource | Free Limit | Your Usage | Status |
|----------|------------|------------|---------|
| Database Size | 500 MB | ~1 KB/day | ✅ Safe (decades of data) |
| API Requests | 500k/month | ~170k/month | ✅ Safe (35% usage) |
| Realtime Connections | 200 concurrent | 2 devices | ✅ Safe |
| Bandwidth | 5 GB/month | Negligible | ✅ Safe |

You won't hit limits! 🎉

---

## ❓ FAQ

**Q: Can I add a third PC?**
A: Yes! Just repeat Step 5 & 6 on PC #3 with the same config.

**Q: What if I want to share with a friend?**
A: Create another user in Supabase dashboard (Authentication → Users → Add user). They'll have separate data.

**Q: Can I see my data in Supabase?**
A: Yes! Dashboard → **Table Editor** → Select `work_sessions` or `task_times`

**Q: What happens to my old local files?**
A: They remain untouched in `%APPDATA%\TrackMvvm\`. The app continues saving locally as backup.

**Q: How do I migrate old data to Supabase?**
A: (Migration tool coming in Phase 5) For now, old data stays local only.

---

## 🎉 You're Done!

Your time tracking app now syncs across all your PCs!

**Next time on a new PC:**
1. Clone repo
2. Copy `appsettings.local.json.template` → `appsettings.local.json`
3. Paste same Supabase URL + key
4. Run app, login once
5. Done!
