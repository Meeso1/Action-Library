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
        public readonly Func<Archive.Entry, bool> check;

        private int lastCheckedEntry;
        private List<Archive.Entry> recentlyReadEvents = new List<Archive.Entry>();

        public bool When()
        {
            bool output = false;
            recentlyReadEvents.RemoveAll(x => true);
            for (int i = lastCheckedEntry + 1; i < Archive.log.Count; i++)
            {
                if (check(Archive.log[i]))
                {
                    output = true;
                    recentlyReadEvents.Add(Archive.log[i]);
                }
                lastCheckedEntry = i;
            }

            return output;
        }
        public List<Archive.Entry> RecentEntries()
        {
            List<Archive.Entry> output = new List<Archive.Entry>();
            recentlyReadEvents.RemoveAll(x => true);
            for (int i = lastCheckedEntry + 1; i < Archive.log.Count; i++)
            {
                if (check(Archive.log[i]))
                {
                    output.Add(Archive.log[i]);
                    recentlyReadEvents.Add(Archive.log[i]);
                }
                lastCheckedEntry = i;
            }

            return output;
        }
        public List<Archive.Entry> GetRecentEntries() { return recentlyReadEvents; }
        public Listener(Func<Archive.Entry, bool> Pcheck, int startIndex, int id)
        {
            check = Pcheck;
            lastCheckedEntry = startIndex;
            this.id = id;
        }
    }
}
