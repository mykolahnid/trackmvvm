using System;
using System.Collections.Generic;
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

        public IEnumerable<WorkSession> GetWorkSessionHistory()
        {
            var workSession = new WorkSession();
            workSession.AddTask("SG");
            yield return workSession;
        }

        public void DeleteHistory()
        {
            
        }
    }
}