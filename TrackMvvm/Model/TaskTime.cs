using System;

namespace TrackMvvm.Model
{
    public class TaskTime
    {
        public string Name { get; set; }

        public double Duration { get; set; }

        public bool IsActive { get; set; }

        public void Start()
        {
            this.IsActive = true;
            OnStart?.Invoke(this.Name);
        }

        public event Action<string> OnStart;
    }
}