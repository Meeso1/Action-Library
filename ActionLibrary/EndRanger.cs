using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionLibrary
{
    public class EndRanger
    {
        public delegate void reactDelegate(Action action);
        public delegate bool scopeDelegate(Action action);

        private static SortedSet<EndRanger> endRangers = new SortedSet<EndRanger>(new SortByPriority());
        private static int firstFreeId = 0;

        public static void CallEndRangers(Action action)
        {
            foreach (EndRanger r in endRangers) r.Call(action);
        }

        public string name = "";
        public double priority;
        private int id;

        private scopeDelegate scope;
        private reactDelegate react;

        public void Call(Action action)
        {
            if (scope(action)) react(action);
        }

        public EndRanger(scopeDelegate scope, reactDelegate react, double priority = 1, string name = "[UNNAMED]")
        {
            this.scope = scope;
            this.react = react;
            this.priority = priority;
            this.name = name;
            id = firstFreeId++;
            endRangers.Add(this);
        }

        //SORTING
        private class SortByPriority : IComparer<EndRanger>
        {
            int IComparer<EndRanger>.Compare(EndRanger a, EndRanger b)
            {
                if (a.priority > b.priority)
                    return 1;
                if (a.priority < b.priority)
                    return -1;
                else
                {
                    if (a.id > b.id)
                        return 1;
                    if (a.id < b.id)
                        return -1;
                    else
                        return 0;
                }
            }
        }
    }
}
