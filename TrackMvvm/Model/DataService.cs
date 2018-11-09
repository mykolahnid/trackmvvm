using System;

namespace TrackMvvm.Model
{
    public class DataService : IDataService
    {
        public void GetData(Action<DataItem, Exception> callback)
        {
            // Use this to connect to the actual data service

            var item = new DataItem("Welcome to MVVM Light");
            callback(item, null);
        }

        public void GetWorkSession(Action<WorkSession, Exception> callback)
        {
            var today = DateTime.Today;
            WorkSession workSession = null;
            //var sess = dataStorageService.getsession(today)
            //if  (sess != null) workSession = sess
            //else workSession = new WorkSession();
                workSession = new WorkSession(today);
            //}

            callback(workSession, null);
        }
    }
}