using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ActionLibrary
{
    public class Ranger
    {
        public delegate bool reactDelegate(Action action, ref Action.Modifiers modifiers);
        public delegate bool scopeDelegate(Action action);

        private static SortedSet<Ranger> rangers = new SortedSet<Ranger>(new SortByPriority());
        private static readonly Dictionary<Action.Group, SortedSet<Ranger>> rangersByGroups = new Dictionary<Action.Group, SortedSet<Ranger>>();
        private static int firstFreeId = 0;

        public static bool CallRangers(Action action, ref Action.Modifiers modifiers)
        {
            bool result = false;

            var rangers = new Extensions.EnumeratorOfSum<Ranger>(new SortedSet<Ranger>());
            if (rangersByGroups.ContainsKey(Action.Group.all)) rangers.Join(rangersByGroups[Action.Group.all]);
            if (rangersByGroups.ContainsKey(action.group)) rangers.Join(rangersByGroups[action.group]);

            foreach (Ranger r in rangers)
            {
                if (r.Call(action, ref modifiers))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        public readonly string name = "";
        public readonly Action.Group group = Action.Group.all;
        public readonly double priority;
        private int id;

        private scopeDelegate scope;
        private reactDelegate react;

        public bool Call(Action action, ref Action.Modifiers modifiers)
        {
            if (!scope(action)) return false;
            return react(action, ref modifiers);
        }

        public Ranger(scopeDelegate scope, reactDelegate react, Action.Group group = null, double priority = 1, string name = "[UNNAMED]")
        {
            this.scope = scope;
            this.react = react;
            this.group = group ?? Action.Group.all;
            this.priority = priority;
            this.name = name;
            id = firstFreeId++;

            rangers.Add(this);
            if (!rangersByGroups.ContainsKey(this.group)) rangersByGroups[this.group] = new SortedSet<Ranger>(new SortByPriority());
            rangersByGroups[this.group].Add(this);
        }

        //SORTING
        private class SortByPriority : IComparer<Ranger>
        {
            int IComparer<Ranger>.Compare(Ranger a, Ranger b)
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

    public partial class Extensions
    {
        public class EnumeratorOfSum<T> : IEnumerable<T>
        {
            public EnumeratorOfSum(SortedSet<T> set)
            {
                sets.Add(set);
            }
            public void AddSet(SortedSet<T> set) => sets.Add(set);

            private List<SortedSet<T>> sets = new List<SortedSet<T>>();
            private List<IEnumerator<T>> currentIterators = new List<IEnumerator<T>>();

            private bool start = true;
            private IEnumerator<T> currentValueEnumerator;

            public void Reset()
            {
                currentIterators.Clear();
                foreach (var s in sets)
                {
                    currentIterators.Add(s.GetEnumerator());
                    start = true;
                }
            }
            
            public IEnumerator<T> GetEnumerator()
            {
                if (start)
                {
                    Reset();

                    var toRemove = new List<IEnumerator<T>>();
                    foreach (var i in currentIterators)
                    {
                        if (!i.MoveNext()) toRemove.Add(i);
                        start = false;
                    }
                    foreach (var i in toRemove) currentIterators.Remove(i);
                }
                else
                {
                    if (!currentValueEnumerator.MoveNext()) currentIterators.Remove(currentValueEnumerator);
                }

                if (currentIterators.Count == 0) yield break;
                currentValueEnumerator = currentIterators[0];
                foreach (var i in currentIterators)
                    if (sets[0].Comparer.Compare(i.Current, currentValueEnumerator.Current) < 0) currentValueEnumerator = i;
                yield return currentValueEnumerator.Current;
            }
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public static EnumeratorOfSum<T> Join<T>(this SortedSet<T> set, SortedSet<T> other)
        {
            var res = new EnumeratorOfSum<T>(set);
            res.AddSet(other);
            return res;
        }
        public static EnumeratorOfSum<T> Join<T>(this EnumeratorOfSum<T> enumerator, SortedSet<T> other)
        {
            enumerator.AddSet(other);
            return enumerator;
        }
    }
}
