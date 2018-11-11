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
            workSession.AddTask("SG");
            workSession.Start("SG");
            callback(workSession, null);
        }
    }
}