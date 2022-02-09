using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionLibrary
{
    public class Listener
    {
        public readonly int id;
        public readonly Func<Log.LogEntry, bool> check;

        private int lastCheckedEntry;
        private List<Log.LogEntry> recentlyReadEvents = new List<Log.LogEntry>();

        public bool When()
        {
            bool output = false;
            recentlyReadEvents.RemoveAll(x => true);
            for (int i = lastCheckedEntry + 1; i < Log.log.Count; i++)
            {
                if (check(Log.log[i]))
                {
                    output = true;
                    recentlyReadEvents.Add(Log.log[i]);
                }
                Log.log[i].SeenBy(id);
                lastCheckedEntry = i;
            }

            return output;
        }
        public List<Log.LogEntry> RecentEntries()
        {
            List<Log.LogEntry> output = new List<Log.LogEntry>(); ;
            recentlyReadEvents.RemoveAll(x => true);
            for (int i = lastCheckedEntry + 1; i < Log.log.Count; i++)
            {
                if (check(Log.log[i]))
                {
                    output.Add(Log.log[i]);
                    recentlyReadEvents.Add(Log.log[i]);
                }
                Log.log[i].SeenBy(id);
                lastCheckedEntry = i;
            }

            return output;
        }
        public List<Log.LogEntry> GetRecentEntries() { return recentlyReadEvents; }
        public Listener(Func<Log.LogEntry, bool> Pcheck, int startIndex, int id)
        {
            check = Pcheck;
            lastCheckedEntry = startIndex;
            this.id = id;
        }
    }
}
