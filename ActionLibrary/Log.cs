using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionLibrary
{
    public static class Log
    {
        public static List<LogEntry> log = new List<LogEntry>();

        private static List<Listener> listeners = new List<Listener>();
        private static int firstFreeLId = 0;

        public static Listener GetListener(Func<LogEntry, bool> check)
        {
            Listener listener = new Listener(check, log.Count - 1, firstFreeLId++);
            listeners.Add(listener);

            return listener;
        }
        public static void AddEntry(Action action, double timestamp, List<string> tags = null)
        {
            log.Add(new LogEntry(action, timestamp, tags));
        }

        public class LogEntry
        {
            public readonly double timestamp;
            public readonly List<string> tags = new List<string>();
            public readonly Action action;

            private SortedSet<int> seenByIds = new SortedSet<int>();

            public LogEntry(Action action, double timestamp, List<string> tags = null)
            {
                this.action = action;
                this.timestamp = timestamp;
                this.tags = tags ?? new List<string>();
            }
            public void SeenBy(int id)
            {
                seenByIds.Add(id);
            }
            public bool WasSeenBy(int id)
            {
                return seenByIds.Contains(id);
            }
        }
    }
}
