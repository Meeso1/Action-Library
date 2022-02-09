using System;
using System.Threading;

namespace ActionLibrary
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Arguments for <c>OnTrigger()</c> methods in actions [UNUSED]
    /// </summary>
    public enum TCode
    {
        sourceDeath,    // source died. Do something
        CCd,            // target was hit by CC that doesn't allow this action. Do something about it (or not, idk)
        irrelevant,     // this action is probably redundant, end it maybe or check if you should (but no pressure)
        cancelled,      // this action was aborted by its source. End it neatly, maybe trigger some aftereffects
        terminate       // instantly end an action ("humanely" - do cleanup and stuff)
    }

    /// <summary>
    /// Arguments for <c>OnTrigger()</c> methods in actions
    /// </summary>
    public sealed partial class ActionCode
    {
        public string name { get; private set; }
        public readonly int id;
        private static int lastId = 0;

        private ActionCode(string name = "[UNNAMED ACTIONCODE]")
        {
            this.name = name;
            this.id = lastId++;
        }

        //Codes
        public static readonly ActionCode sourceDeath = new ActionCode("Source death");
        public static readonly ActionCode CCd = new ActionCode("CCd");
        public static readonly ActionCode irrelevant = new ActionCode("Irrelevant");
        public static readonly ActionCode cancelled = new ActionCode("Cancelled");
        public static readonly ActionCode terminate = new ActionCode("Terminate");
    }

    /// <summary>
    /// Represents an activity that does something after the delay (i.e. after 2s, deal damage to the player),
    /// or causes a continous effect (i.e. deal damage to the player every second for 5s).
    /// </summary>
    public abstract class Action
    {
        public const bool DEBUGMESSAGES = false;

        //STATE TAGS
        public bool frozen { get; protected set; } = false;
        public bool ethernal { get; protected set; } = false;
        public bool archived { get; private set; } = false;
        public readonly bool instant = false;
        public readonly bool deniable = true;
        public readonly bool reactable = true;

        private bool onEndWasCalled = false;
        protected double _totalDuration;
        private double lastUpdateTime = -1;

        public readonly Group group = Group.ungrouped;

        //PROPERTIES
        public double totalDuration { get => _totalDuration; set => ChangeTotalDuration(value); }
        public double duration { get; protected set; }

        private static readonly List<Action> actions = new List<Action>();

        //CONSTRUCTORS
        public class Args
        {
            public double duration = 0;
            public bool ethernal = false;           //Ethernal actions don't change their duration during updates, 
                                                    //and don't end when duration hits 0.
            public bool frozen = false;             //Frozen actions are not updated until they are unfrozen.
            public bool instant = false;            //Instant actions are never updated and end right after they start.
            public Group group = Group.ungrouped;
            public bool deniable = true;                   //Deniable action can be denied start by rangers.
            public bool reactable = true;                  //Rangers can react to reactable actions.
        }
        public Action(Args a) : this(a.duration, a.ethernal, a.frozen, a.instant)
        {
            group = a.group;
            reactable = a.reactable;
            deniable = a.deniable;
        }
        public Action( //To assign a Group, use an Args constructor
            double duration = 0,
            bool ethernal = false,
            bool frozen = false,
            bool instant = false
            )
        {
            _totalDuration = duration;
            this.ethernal = ethernal;
            this.frozen = frozen;
            this.instant = instant;
        }

        public virtual void OnStart() { }
        public virtual void OnEnd() { }
        public virtual void OnUpdate(double dtime) { }
        public virtual void OnTrigger(ActionCode code)
        {
            switch (code)
            {
                case ActionCode _ when code == ActionCode.terminate:
                    RemoveAction();
                    break;
                default:
                    break;
            }
        }
        public virtual void OnTerminate() { }

        //MODIFYING AN ACTION (done just after creation)
        public class Modifiers
        {
            //Some attributes...
            public Dictionary<string, object> strAttributes = new Dictionary<string, object>();
        }
        public virtual Modifiers GetModifiers() => new Modifiers();
        public virtual void Modify(Modifiers modifiers) { }

        //GROUPS
        public class Group
        {
            public readonly string name = "UNNAMED";

            private static readonly Dictionary<string, Group> groupsByName = new Dictionary<string, Group>();

            public static Group all = new Group("All");
            public static Group ungrouped = new Group("Ungrouped");

            private Group(string name)
            {
                if (groupsByName.ContainsKey(name)) throw new System.Exception($"Group with name {name} already exists! Can't create another one.");
                this.name = name;
                groupsByName[name] = this;
            }
            public static Group NewGroup(string name) => new Group(name);
            public static Group Get(string name)
            {
                if (!groupsByName.ContainsKey(name)) return null;
                return groupsByName[name];
            }
        }

        //ACTION LIFE CYCLE
        public static bool AddAction(Action action)
        {
            if (action is null) return true;

            lock (staticKey)
            {
                if (actions.Contains(action)) throw new System.Exception($"Attempted to add Action that was already added: {action.GetType()}");

                var modifiers = action.GetModifiers();
                bool rangersDenied = false;
                if (action.reactable) rangersDenied = Ranger.CallRangers(action, ref modifiers);
                if (!rangersDenied || !action.deniable)
                {
                    action.Modify(modifiers);
                    action.duration = action.totalDuration;
                    actions.Add(action);
                    if (!action.instant) Message(action.StartMessage());
                    action.OnStart();
                    action.lastUpdateTime = GetTime();
                    if (action.instant) action.EndAction();
                    return true;
                } 
            }
            return false;
        }
        public void EndAction()
        {
            lock (staticKey)
            {
                OnEnd();
                onEndWasCalled = true;
                RemoveAction();
            }
        }
        public void RemoveAction(bool callEndRangers = true)
        {
            lock (staticKey)
            {
                if (!onEndWasCalled) OnTerminate();
                Message(EndMessage());
                if (callEndRangers) EndRanger.CallEndRangers(this);
                archived = true;
                //actions.Remove(this); //archieved actions are removed from actions list at the end of each update cycle.
                Log.AddEntry(this, GetTime()); 
            }
        }
        public static void UpdateActions(double dtime)
        {
            List<Action> toRemove = new List<Action>();

            for (int i = 0; i < actions.Count; i++)
            {
                if (!actions[i].frozen)
                {
                    if (!actions[i].ethernal)
                    {
                        if (actions[i].duration <= dtime)
                        {
                            double durationLeft = actions[i].duration;
                            actions[i].duration = 0;
                            toRemove.Add(actions[i]);
                            actions[i].OnUpdate(durationLeft);
                        }
                        else
                        {
                            actions[i].duration -= dtime;
                            actions[i].OnUpdate(dtime);
                        }
                    }
                    else actions[i].OnUpdate(dtime);
                }
            }

            foreach (var a in toRemove) a.EndAction();
        }//TODO: Remove
        public static void InsertAction(Action action)
        {
            lock (staticKey)
            {
                actions.Add(action);
                if (action.instant) action.EndAction();
            }
        }
        public void DeleteAction()
        {
            lock (staticKey)
            {
                if (!onEndWasCalled) OnTerminate();
                actions.Remove(this);
            }
        }

        //UTILITY
        public delegate bool ActionEval(Action a);
        private static double GetTime()
        {
            return (DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds;
        }
        public Action Clone()
        {
            lock(key) return (Action)MemberwiseClone();
        }
        protected virtual void ChangeTotalDuration(double newTotalDuration, bool addAhead = true)
        {
            lock (key)
            {
                if (addAhead)
                {
                    duration += (newTotalDuration - totalDuration);
                    if (duration < 0) duration = 0;
                }
                _totalDuration = newTotalDuration; 
            }
        }
        public void SetFrozen(bool b)
        {
            lock(key) frozen = b;
        }
        public void SetEthernal(bool b) 
        { 
            lock(key) ethernal = b; 
        }
        public static void Shutdown() => stop = true;

        //LOCKING & THREADS
        public object key = new object(); //lock on this object to assure that this action won't be updated during the operation.
        private static object staticKey = new object(); //locked to prevent updates during OnStart() and OnEnd() action methods.
        private static Thread updateThread;
        private static bool stop = false;
        private static void UpdateActionsThreadMethod()
        {
            while (!stop) UpdateActionsOnce();
            TerminateAllActions();
        }
        private static void UpdateActionsOnce()
        {
            List<Action> toRemove = new List<Action>();

            for (int i = 0; i < actions.Count; i++) lock(staticKey) lock(actions[i].key)
            {
                if (!actions[i].frozen && !actions[i].archived)
                {
                    double now = GetTime();
                    double dtime = now - actions[i].lastUpdateTime;
                    if (!actions[i].ethernal)
                    {
                        if (actions[i].duration <= dtime)
                        {
                            double durationLeft = actions[i].duration;
                            actions[i].duration = 0;
                            toRemove.Add(actions[i]);
                            actions[i].OnUpdate(durationLeft);
                        }
                        else
                        {
                            actions[i].duration -= dtime;
                            actions[i].OnUpdate(dtime);
                        }
                    }
                    else actions[i].OnUpdate(dtime);
                    actions[i].lastUpdateTime = now;
                }
            }

            foreach (var a in toRemove) lock(a.key) a.EndAction();
            lock(staticKey) actions.RemoveAll((a) => a.archived);
        }
        private static void TerminateAllActions()
        {
            lock (staticKey)
            {
                foreach (Action a in actions) a.RemoveAction();
            }
        }
        static Action()
        {
            updateThread = new Thread(UpdateActionsThreadMethod);
            updateThread.Start();
        }

        //GET CERTAIN ACTIONS
        public static List<Action> GetAll(ActionEval check)
        {
            List<Action> result = new List<Action>();
            lock(staticKey) foreach (Action action in actions) if (check(action) && !action.archived) result.Add(action);
            return result;
        }
        public static Action Get(ActionEval check)
        {
            lock(staticKey) foreach (Action action in actions) if (check(action) && !action.archived) return action;
            return null;
        }
        public static bool Any(ActionEval check)
        {
            lock(staticKey) foreach (Action action in actions) if (check(action) && !action.archived) return true;
            return false;
        }

        //BASIC LOG MESSAGES
        public static void Message(string message, bool noRequireDebug = false)
        {
            if (DEBUGMESSAGES || noRequireDebug) Console.WriteLine(message);
        }//TODO: Detach
        public virtual string StartMessage() { return ""; }
        public virtual string EndMessage() { return ""; }
        public virtual string TriggerRemoveMessage() { return ""; }
    }

    public static partial class Extensions
    {
        public static T Start<T>(this T a) where T : Action
        {
            Action.AddAction(a);
            return a;
        }
    }

    public class Await : Action
    {
        public delegate bool AwaitTarget();
        public delegate void Reaction();

        protected AwaitTarget awaitTarget;
        protected Reaction reaction = () => { };

        public Await(AwaitTarget a) : base(ethernal: true) { awaitTarget = a; }

        public override void OnUpdate(double dtime)
        {
            //Console.WriteLine($"Await update (dtime: {dtime}, duration: {duration})");
            if (awaitTarget())
            {
                reaction();
                OnTrigger(ActionCode.terminate);
            }
        }
        public Await Then(Reaction reaction) { this.reaction = reaction; return this; }
    }

    public class Delay : Action
    {
        public delegate void Reaction();

        protected Reaction reaction = () => { };

        public Delay(double time) : base(duration: time) { }

        public override void OnStart()
        {
            //Console.WriteLine($"Delay start (duration: {duration} [{totalDuration}])");
        }

        public override void OnUpdate(double dtime)
        {
            //Console.WriteLine($"Delay action update [dtime: {dtime}, duration: {duration}]");
        }

        public override void OnEnd() { reaction(); }
        public Delay Then(Reaction reaction) { this.reaction = reaction; return this; }
    }

    public class Post : Action
    {
        public readonly string name;
        public readonly object inside;

        public Post(string name, object o, Group group = null) : base(new Args() { instant = true, group = group })
        {
            this.name = name;
            inside = o;
        }
    }

    public class Signal : Action
    {
        public readonly string name;
        public readonly Dictionary<string, object> details;

        public Signal(string name, Dictionary<string, object> o, Group group = null) : base(new Args() { instant = true, group = group })
        {
            this.name = name;
            details = o;
        }
    }

}
