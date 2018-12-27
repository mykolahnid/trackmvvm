using System;
using System.IO;

namespace TrackMvvm.Model
{
    public class DataService : IDataService
    {
        private const string Prefix = "trackmvvm ";
        private const string DateFormat = "yyyy-MM-dd";

        public void GetWorkSession(Action<WorkSession, Exception> callback)
        {
            WorkSession workSession = null;
            //var sess = dataStorageService.getsession(today)
            //if  (sess != null) workSession = sess
            //else workSession = new WorkSession();
                workSession = new WorkSession();
            workSession.AddTask("SG");
            workSession.AddTask("APMS");
            //workSession.Start("SG");
            //}

            callback(workSession, null);
        }

        public void SaveWorkSession(WorkSession workSession)
        {
            var sessionString = workSession.Serialize();

            var trackDirectory = GetTrackDirectory();

            try
            {
                File.WriteAllText(
                    Path.Combine(trackDirectory, Prefix + workSession.Today.ToString(DateFormat) + ".txt"),
                    sessionString);
            }
            catch
            {
            }
        }

        private static string GetTrackDirectory()
        {
            string applicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string trackDirectory = Path.Combine(applicationDataDirectory, "TrackMvvm");
            if (!Directory.Exists(trackDirectory))
            {
                Directory.CreateDirectory(trackDirectory);
            }
            return trackDirectory;
        }
    }
}