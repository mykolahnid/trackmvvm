using System.Collections.Generic;
using System.Collections.ObjectModel;
using TrackMvvm.Model;
using TrackMvvm.Utilities;

namespace TrackMvvm.ViewModel
{
    public class WorkSessionHistoryViewModel
    {
        private const string HoursFormat = "0.0";

        public void SetWorkSessionHistory(IEnumerable<WorkSession> workSessionHistory)
        {
            History = new ObservableCollection<BindableDynamicDictionary>();

            foreach (var workSession in workSessionHistory)
            {
                var historyItem = new BindableDynamicDictionary();
                historyItem["Date"] = workSession.Today.ToString("ddd dd MMM");
                var total = 0d;
                foreach (var workSessionTask in workSession.Tasks)
                {
                    historyItem[workSessionTask.Name] = workSessionTask.DurationHours.ToString(HoursFormat);
                    total += workSessionTask.DurationHours;
                }

                historyItem["Total"] = total.ToString(HoursFormat);
                History.Add(historyItem);
            }
        }

        public ObservableCollection<BindableDynamicDictionary> History { get; set; }
    }
}
