using System;
using TrackMvvm.Model;

namespace TrackMvvm.Design
{
    public class DesignDataService : IDataService
    {
        public void GetData(Action<DataItem, Exception> callback)
        {
            // Use this to create design time data

            var item = new DataItem("Welcome to MVVM Light [design]");
            callback(item, null);
        }

        public void GetWorkSession(Action<WorkSession, Exception> callback)
        {
            throw new NotImplementedException();
        }
    }
}