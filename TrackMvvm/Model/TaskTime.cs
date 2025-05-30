using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace TrackMvvm.Model
{
    public class TaskTime : INotifyPropertyChanged
    {
        private bool _isActive;
        private double _duration;

        [DataMember]
        public string Name { get; set; }

        [DataMember]
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

        [XmlIgnore]
        public double DurationHours => Math.Round(Duration / 60.0 / 60.0, 1);

        [XmlIgnore]
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

        public void Remove()
        {
            OnRemove?.Invoke(this.Name);
        }

        public event Action<string> OnStart;
        public event Action<string> OnRemove;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}