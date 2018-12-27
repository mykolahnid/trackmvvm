using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using TrackMvvm.Annotations;

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