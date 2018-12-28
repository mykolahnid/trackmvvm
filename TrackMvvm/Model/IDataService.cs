using System;
using System.Collections.Generic;

namespace TrackMvvm.Model
{
    public interface IDataService
    {
        void GetWorkSession(Action<WorkSession, Exception> callback);
        void SaveWorkSession(WorkSession workSession);
        IEnumerable<WorkSession> GetWorkSessionHistory();
    }
}
