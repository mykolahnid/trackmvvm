using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using TrackMvvm.Contracts;
using TrackMvvm.Properties;

namespace TrackMvvm.Helpers
{
    class Bootstrapper
    {
        public IList<ITrackPlugin> Plugins
        {
            get;
            private set;
        }

        public void Refresh()
        {
            Plugins = new List<ITrackPlugin>();

            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), (string)Settings.Default["PluginFolder"]);

            var folder = new DirectoryInfo(folderPath);

            foreach (var file in folder.GetFiles("*.dll"))
            {
                var assembly = Assembly.LoadFrom(file.FullName);
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(ITrackPlugin).IsAssignableFrom(type) && !type.IsInterface)
                    {
                        var constructor = type.GetConstructor(
                            new Type[]
                                { });

                        if (constructor == null) throw new NullReferenceException();

                        var manager = constructor.Invoke(
                            new object[]
                                { });

                        Plugins.Add((ITrackPlugin)manager);
                    }
                }
            }
        }
    }
}
