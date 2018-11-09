using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrackMvvm.Model
{
    public interface IDataService
    {
        void GetData(Action<DataItem, Exception> callback);
        void GetWorkSession(Action<WorkSession, Exception> callback);
    }
}
