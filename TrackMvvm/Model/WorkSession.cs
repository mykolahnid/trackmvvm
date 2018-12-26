using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Timers;

namespace TrackMvvm.Model
{
    public class WorkSession
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly Timer timer;
        public DateTime Today;
        private TaskTime activeTask;
        private TimeSpan rememberedDuration;

        public event EventHandler<TaskTime> TaskAdded;
        public event Action<string> TaskStarted;

        public List<TaskTime> Tasks { get; set; }

        public TimeSpan TotalDuration => stopwatch.Elapsed;

        public WorkSession(DateTime today)
        {
            Today = today;
            Tasks = new List<TaskTime>();
            timer = new Timer(100);
            timer.Elapsed += timer_Elapsed;
            timer.Start();
            stopwatch.Start();
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (activeTask != null)
            {
                activeTask.Duration = activeTask.Duration += (stopwatch.Elapsed - rememberedDuration).TotalSeconds;
            }
            rememberedDuration = stopwatch.Elapsed;
        }

        public void AddTask(string name)
        {
            name = name.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                if (Tasks.All(t => t.Name.ToLower() != name.ToLower()))
                {
                    var taskTime = new TaskTime() { Name = name };
                    taskTime.OnStart += Start;
                    Tasks.Add(taskTime);
                    TaskAdded?.Invoke(null, Tasks[Tasks.Count - 1]);
                }
            }
        }

        public void Start(string name)
        {
            if (activeTask != null)
            {
                activeTask.IsActive = false;
            }
            foreach (TaskTime task in Tasks)
            {
                if (task.Name == name)
                {
                    activeTask = task;
                    activeTask.IsActive = true;
                    TaskStarted?.Invoke(name);
                    break;
                }
            }
        }

        public void Stop()
        {
            if (activeTask != null)
            {
                activeTask.IsActive = false;
            }
            activeTask = null;
        }
    }
}