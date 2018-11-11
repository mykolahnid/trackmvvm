using System;

namespace TrackMvvm.Model
{
    public class DataService : IDataService
    {
        public void GetWorkSession(Action<WorkSession, Exception> callback)
        {
            var today = DateTime.Today;
            WorkSession workSession = null;
            //var sess = dataStorageService.getsession(today)
            //if  (sess != null) workSession = sess
            //else workSession = new WorkSession();
                workSession = new WorkSession(today);
            workSession.AddTask("SG");
            workSession.AddTask("APMS");
            workSession.Start("SG");
            //}

            callback(workSession, null);
        }
    }
}