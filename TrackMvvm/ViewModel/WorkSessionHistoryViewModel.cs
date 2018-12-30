using System.Collections.ObjectModel;
using TrackMvvm.Model;
using TrackMvvm.Utilities;

namespace TrackMvvm.ViewModel
{
    public class WorkSessionHistoryViewModel
    {
        private readonly IDataService _dataService;

        public WorkSessionHistoryViewModel(IDataService dataService)
        {
            _dataService = dataService;
            History = new ObservableCollection<BindableDynamicDictionary>();

            var workSessionHistory = _dataService.GetWorkSessionHistory();
            foreach (var workSession in workSessionHistory)
            {
                var historyItem = new BindableDynamicDictionary();
                historyItem["Date"] = workSession.Today.ToString("ddd dd MMM");
                foreach (var workSessionTask in workSession.Tasks)
                {
                    historyItem[workSessionTask.Name] = workSessionTask.Duration;
                }
                History.Add(historyItem);
            }
        }

        public ObservableCollection<BindableDynamicDictionary> History { get; set; }
    }
}
