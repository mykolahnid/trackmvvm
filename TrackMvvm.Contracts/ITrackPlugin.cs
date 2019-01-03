using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackMvvm.Contracts
{
    public interface ITrackPlugin
    {
        string Name { get; }
        /*
         * add ITrackTime inteface
         * Add some public methods with TrackTime arg
         * RelayCommands?
         * get them all, add as menu items to context menu
         * maybe first test with class in main app
         * make a view model that will add "Remove" as first item
         * other plugin commands below. don't split, we won't have much plugins. 
         *
         *
         *
         */
    }
}
