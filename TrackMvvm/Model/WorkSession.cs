using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Timers;
using System.Xml.Serialization;

namespace TrackMvvm.Model
{
    public class WorkSession
    {
        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly Timer timer;
        [DataMember] public DateTime Today = DateTime.Today;
        private TaskTime activeTask;
        private TimeSpan rememberedDuration;

        public event EventHandler<TaskTime> TaskAdded;
        public event Action<string> TaskStarted;

        [DataMember]
        public List<TaskTime> Tasks { get; set; }

        [DataMember]
        public TimeSpan TotalDuration => stopwatch.Elapsed;

        public WorkSession()
        {
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

        public string Serialize()
        {
            var textWriter = new StringWriter();
            var xmlSerializer = new XmlSerializer(typeof (WorkSession));
            xmlSerializer.Serialize(textWriter, this);
            return textWriter.ToString();
        }

        public static WorkSession Deserialize(string serialized)
        {
            var xmlSerializer = new XmlSerializer(typeof (WorkSession));

            WorkSession deserialized = null;
            using (TextReader reader = new StringReader(serialized))
            {
                try
                {
                    deserialized = xmlSerializer.Deserialize(reader) as WorkSession;                    
                }
                catch
                {
                }
            }

            return deserialized;
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