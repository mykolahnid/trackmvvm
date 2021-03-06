﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace TrackMvvm.Model
{
    public class DataService : IDataService
    {

#if DEBUG
        private const string PREFIX = "trackmvvmTEST ";
#else
        private const string PREFIX = "trackmvvm ";
#endif
        private const string DATE_FORMAT = "yyyy-MM-dd";

        private static DateTime GetTrackDate(string s)
        {
            DateTime.TryParseExact(s.Substring(s.IndexOf(PREFIX) + PREFIX.Length, DATE_FORMAT.Length), DATE_FORMAT,
                new CultureInfo("en-US"), DateTimeStyles.None, out var dateTime);
            return dateTime;
        }

        public void GetWorkSession(Action<WorkSession, Exception> callback)
        {
            string trackDirectory = GetTrackDirectory();
            string[] sessionFiles = Directory.GetFiles(trackDirectory);
            var mostRecentTrackDate = new DateTime(1970, 1, 1);
            string mostRecentTrackFile = "";
            WorkSession session = null;
            foreach (string fileName in sessionFiles)
            {
                var dateTime = GetTrackDate(fileName);
                if (dateTime > mostRecentTrackDate)
                {
                    mostRecentTrackDate = dateTime;
                    mostRecentTrackFile = fileName;
                }
            }

            if (mostRecentTrackFile != "")
            {
                var deserialized = WorkSession.Deserialize(File.ReadAllText(mostRecentTrackFile));
                if (deserialized != null)
                {
                    if (deserialized.Today.Date != DateTime.Today.Date)
                    {
                        foreach (var task in deserialized.Tasks)
                        {
                            task.Duration = 0;
                        }
                        deserialized.Today = DateTime.Today;
                    }
                    else
                    {
                        foreach (var task in deserialized.Tasks)
                        {
                            deserialized.TotalTasksDuration += task.Duration;
                        }
                    }
                }
                session = deserialized;
            }
            if (session == null)
            {
                session = new WorkSession();
            }

            callback(session, null);
        }

        public void SaveWorkSession(WorkSession workSession)
        {
            var sessionString = workSession.Serialize();

            var trackDirectory = GetTrackDirectory();

            try
            {
                File.WriteAllText(
                    Path.Combine(trackDirectory, PREFIX + workSession.Today.ToString(DATE_FORMAT) + ".txt"),
                    sessionString);
            }
            catch
            {
            }
        }

        public IEnumerable<WorkSession> GetWorkSessionHistory()
        {
            var trackDirectory = GetTrackDirectory();

            var sessionFiles = Directory.GetFiles(trackDirectory);
            foreach (var s in sessionFiles)
            {
                var deserialized = WorkSession.Deserialize(File.ReadAllText(s));
                if (deserialized != null)
                {
                    yield return deserialized;
                }
            }
        }

        public void DeleteHistory()
        {
            string trackDirectory = GetTrackDirectory();

            string[] sessionFiles = Directory.GetFiles(trackDirectory);
            foreach (var sessionFile in sessionFiles)
            {
                File.Delete(sessionFile);
            }
        }

        private static string GetTrackDirectory()
        {
            string applicationDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
#if DEBUG
            string trackDirectory = Path.Combine(applicationDataDirectory, "TrackMvvmTest");
#else
            string trackDirectory = Path.Combine(applicationDataDirectory, "TrackMvvm");
#endif

            if (!Directory.Exists(trackDirectory))
            {
                Directory.CreateDirectory(trackDirectory);
            }

            return trackDirectory;
        }
    }
}