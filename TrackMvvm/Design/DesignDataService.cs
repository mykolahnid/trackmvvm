using System;
using TrackMvvm.Model;

namespace TrackMvvm.Design
{
    public class DesignDataService : IDataService
    {
        public void GetWorkSession(Action<WorkSession, Exception> callback)
        {
            var today = DateTime.Today;
            var workSession = new WorkSession(today);
            workSession.Tasks.Add(new TaskTime() { Duration = 1, IsActive = false, Name = "SG" });
            callback(workSession, null);
        }
    }
}