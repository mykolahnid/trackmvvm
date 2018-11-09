namespace TrackMvvm.ViewModel
{
    public class TaskTimeViewModel
    {
        public string Name { get; set; }

        public double Duration { get; set; }

        public string Color { get; set; }

        public bool Active { get; set; }

        public string DurationHours => (Duration / 60.0 / 60.0).ToString("0.0");
    }
}