using System;

namespace TrackMvvm.Model
{
    public interface IDataService
    {
        void GetWorkSession(Action<WorkSession, Exception> callback);
    }
}
