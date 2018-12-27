using System;
using TrackMvvm.Model;

namespace TrackMvvm.Design
{
    public class DesignDataService : IDataService
    {
        public void GetWorkSession(Action<WorkSession, Exception> callback)
        {
            var workSession = new WorkSession();
            workSession.AddTask("SG");
            workSession.Start("SG");
            callback(workSession, null);
        }

        public void SaveWorkSession(WorkSession workSession)
        {
            
        }
    }
}