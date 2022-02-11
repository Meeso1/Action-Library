# Action-Library

This library provides implementation of the Action class. 
Actions are objects that are automatically periodically updated. 
Their creation (and deletion) can also be monitored and reacted to, using Ranger and EndRanger classes.

Action class provides 3 special methods to be overridden: OnStart(), OnUpdate(), and OnEnd(). 
When the Action is started, every Ranger object (which is interested in this Action) is called. 
This may result in modifying the Action parameters (Modify() and Action.Modifiers) or even in denying of action start whatsoever (if Action.deniable is true).
After that, OnStart() is called to setup whatever this Action needs to set up.

After creation, all actions are cyclically updated in the separate thread, for a total time that's determined by an Action totalDuration parameter.
After that, OnEnd() is called, then every EndRanger is notified, and at last Action is archived.
