using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionLibrary
{
    public static class Archive
    {
        public static List<Entry> log = new List<Entry>();

        private static List<Listener> listeners = new List<Listener>();
        private static int firstFreeLId = 0;

        public static Listener GetListener(Func<Entry, bool> check)
        {
            Listener listener = new Listener(check, log.Count - 1, firstFreeLId++);
            listeners.Add(listener);

            return listener;
        }
        public static void AddEntry(Action action, double timestamp, List<string> tags = null)
        {
            log.Add(new Entry(action, timestamp, tags));
        }

        public class Entry
        {
            public readonly double timestamp;
            public readonly List<string> tags = new List<string>();
            public readonly Action action;

            public Entry(Action action, double timestamp, List<string> tags = null)
            {
                this.action = action;
                this.timestamp = timestamp;
                this.tags = tags ?? new List<string>();
            }
        }
    }
}
