using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using TrackMvvm.Model;

namespace TrackMvvm.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        public ObservableCollection<TaskTimeViewModel> TasksCollection { get; set; } = new ObservableCollection<TaskTimeViewModel>();

        private WorkSession _workSession;

        public WorkSession WorkSession
        {
            get => _workSession;
            set => Set(ref _workSession, value);
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            _dataService = dataService;
            
            _dataService.GetWorkSession(
                (item, error) =>
                {
                    if (error != null)
                    {
                        return;
                    }

                    WorkSession = item;
                    foreach (var t in WorkSession.Tasks)
                    {
                        var taskTimeViewModel = new TaskTimeViewModel(t);
                        TasksCollection.Add(taskTimeViewModel);
                    }
                });
        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}