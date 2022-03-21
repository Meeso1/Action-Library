# Action-Library

### Introduction
This library provides implementation of the Action class. 
Actions are objects that are automatically periodically updated independently to synchronous code.
Their creation (and deletion) can also be monitored and reacted to, using Ranger and EndRanger classes.
They can be thought of as the extension of async-await and events in C#.

### Most important methods
Action class provides 3 special methods to be overridden: OnStart(), OnUpdate(), and OnEnd(). 
OnStart() is run once when the action is started (using a static AddAction() method, or .Start() extension method). It is guaranteed that this method will not be executed in parallel with any OnUpdate() call, meaning that it is safe to acces any other action in it, without using locks.

OnUpdate() method is periodically executed in an update loop that runs in a separate thread. It takes one argument (double dt) which specifies the time that passed since the last OnUpdate() call for this action. It is guaranteed that at most one OnUpdate() method is executed at any given time, meaning that it is safe to acces any other action in it. It is guaranteed that the sum of dt arguments of all OnUpdate() calls for a given action will be equal (within margin of error caused by floating point arithmetic) to a duration argument specified during action creation (this argument specifies the time after which the action ends).

OnEnd() is executed just after the last OnUpdate() call, when the time specified by a duration constructor argument passes. If an action is stopped by different means (i.e. OnTrigger(ActionCode.Terminate) call), this method is (usually) not executed. Just as OnStart(), during the execution of this method no action shall be updated.

### Creation
Action class is abstract. To use it, make a class that inherits from it and delegates its constructor, or use the few utility action classes provided with this library (more about them at the end of this file).
Action class specifies 2 constructors. The first one takes a duration argument (specifying the total time from the start of this action to the end), and 3 bool arguments which can enable a special behavior. 
The second constructor takes one argument - an Args object. It contains fields corresponding to the arguments of the first constructor, as well as a few more. The purpose of it is constructor argument delegation. In an inheritance chain of internal classes that derive from Action, it may be easier to specify their parameters as an object of a class that inherits from Args. This way a constructor parameter can be directly passed to a base() constructor.

### Special behavior
The constructor takes a few bool parameters which specify a special behavior of a class that is being created. Actions can be:
- instant - An instant action ends right after creation. OnUpdate() is never called for this type of action.
- ethernal - Ethernal actions never end (unless explicitly terminated with OnTrigger(ActionCode.Terminate) call). They are updated normally, although the duration of the action is not decremented during update.
- frozen - A frozen action is not updated and it's remaining duration doesn't change. The action can be frozen and unfrozen at any time.
- reactable - Determines wether rangers (see Reacting to actions section) will be notified of this action's start and end.
- deniable - Determines wether rangers will be able to deny the start of this action.

### Reactiong to actions
An important aspect of actions is the possibility of reacting to their start and end. It is accomplished with the use of Ranger and EndRanger objects.

A Ranger constructor takes the following arguments:
- scope - a function (Action => bool) that specifies which actions this ranger will react to (if scope(a) returns true, a ranger will react to an action a).
- react - a function (Action, Modifiers => bool) that specifies the reaction. If this function returns true (and the action is deniable) the action won't be started, and no subsequent rangers will react to this action. Modifiers object is a ref argument that contains parameters that can be used to modify the state of the action before the OnStart() call. The class inheriting from Action can override a GetModifiers() method to return an instance of the class that inherits from Modifiers class, with fields that determine what aspects of an action can be modified (the modification is done after every interested ranger has reacted to the action creation, with a call to a Modify() method of the action, with a Modifiers argument created by rangers). 
- priority - specifies the order in which rangers will react to actions.
- group (optional) - a group of actions that this ranger will react to. The group is checked before the scope. An action can belong to a group determined by a constructor parameter (can only be set with Args constructor). If this argument is omitted, the ranger will react to every action that is started. The purpose of groups is to improve complexity scaling. Without them adding a new action is O([number of rangers]), while using them reduces it to O([number of rangers in a group]), which can be easier to control.

It is also possible to react to the termination of an action. It is analogous to the procedure described above, and uses EndRanger objects instead of rangers.
The only difference between them is that (for obvious reasons) endrangers cannot modify the creation of the action, and cant deny its creation. Because of this, the react parameter is a (Action => void) function.

### Included simple utility actions
This library includes a few utility action classes.
- Delay - executes a function after a delay. Usage: new Delay([duration]).Then([function]).Start();
- Await - in every update checks if a condition is true, and if so, executes a function. Usage: new Await([(void => bool) condition]).Then([function]).Start(); This can be used to execude some function just after the current OnStart()/OnUpdate()/OnEnd() call, by passing ()=>true as a condition.
- Signal - instant action. Signalizes that something happened. With rangers, usage of signals can be equivalent to raising C# events (but instead of subscribing to an event, the object wishing to react to an event needs to create a ranger). Contains a <string, object> dictionary with details of the event signaled.
- Post - similar to signal, but used to pass data/references easily. Contains an object that is being published, which can be captured by rangers.

### Various remarks
Actions contain more interesting methods than described above. For example:
- InsertAction()/DeleteAction() - add/remove actions to/from the list of actions that are being updated, but don't call OnStart()/OnEnd() methods and don't trigger rangers/endrangers.
- OnTrigger() - an overridable method that can be used to pass simple triggers to an action. Takes an ActionCode argument (an enum-like partial class) which describes a type of trigger. This can be used to i.e. implement action cancellation, or some lazy computation solutions. The most common use of this method is the termination of a certain action by a OnTrigger(ActionCode.Terminate) call. It is an only argument that is not ignored by a default implementation of that method, and every override of this action should terminate (call RemoveAction() internally) in reaction to this argument. Also by convention frozen actions should ignore OnTrigger() calls with all arguments that are not ActionCode.Terminate.
- Get()/GetAll()/Any() - get actions that satisfy some conditions, or information if such actions exist.
- Various message methods - return strings that are written to Console at the start and end of the action if DEBUGMESSAGES is set to true in Action class. By default return empty strings.
- OnTerminate() - a method called after the action is being terminated, if OnEnd() will not be called. Can be used to perform some cleanup that would be normally done in OnEnd().

As described above, actions are continously updated in a separate thread. It means that accessing members of an action from the synchronous code may cause race conditions in some cases (if a value that is being accessed can be changed during OnUpdate() or OnEnd() calls). To avoid this, actions contain a key object. It is locked before anything is done involving this action in the action update thread. Locking this key will guarantee that all subsequent operations on an action will be thread-safe. 

Action class also contains a staticKey object, which is locked before anything is done with any of the actions. Locking this key effectively suspends the execution of action update thread, meaning that any operations involving action members become thread-safe. It is generally not recommended to use this approach, although it is possible and used internally.

After the action ends, the object is not deleted, it just stops being updated and is removed from every internal static action list used for different functionalities. That means that if a reference to an action object is stored somewhere, accessing of this action can be done after the action ha ended. To check if the action has ended, the value of archived property should be examined (true -> action ended).
