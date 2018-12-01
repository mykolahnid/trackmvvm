using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TrackMvvm.Annotations;

namespace TrackMvvm.Model
{
    public class TaskTime : INotifyPropertyChanged
    {
        private bool _isActive;
        private double _duration;
        public string Name { get; set; }

        public double Duration
        {
            get => _duration;
            set
            {
                if (value.Equals(_duration)) return;
                _duration = value;
                OnPropertyChanged();
            }
        }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (value == _isActive) return;
                _isActive = value;
                OnPropertyChanged();
            }
        }

        public void Start()
        {
            this.IsActive = true;
            OnStart?.Invoke(this.Name);
        }

        public event Action<string> OnStart;
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}