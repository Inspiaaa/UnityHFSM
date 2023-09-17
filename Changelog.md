# Changelog

---

## 2.0

### Added

- **Ghost states**: Ghost states are states that the state machine does not want to remain in and will try to exit as soon as possible. This means that the fsm can do multiple transitions in one `OnLogic` call. The "ghost state behaviour" is supported by all state types by setting the `isGhostState` field.
  
  E.g.
  
  ```csharp
  fsm.AddState("A", onEnter: s => print("A"));
  fsm.AddState("B", new State(onEnter: s => print("B"), isGhostState: true));
  fsm.AddState("C", onEnter: s => print("C");
  
  fsm.AddTransition("A", "B");
  fsm.AddTransition("B", "C");
  
  fsm.Init(); // Prints "A"
  fsm.OnLogic(); // Prints "B" and then "C"
  ```

- **Exit transitions**: Exit transitions finally provide an easy and powerful way to define the exit conditions for nested state machines, essentially levelling up the mechanics behind hierarchical state machines. Previously, the rule that determined when a nested state machine that `needsExitTime` can exit, was implicit, not versatile, and not in the control of the developer.
  
  ```csharp
  var nested = new StateMachine(needsExitTime: true);
  nested.AddState("A");
  nested.AddState("B");
  // ...
  
  // The nested fsm can only exit when it is in the "B" state and
  // the variable x equals 0.
  move.AddExitTransition("B", t => x == 0);
  ```
  
  Exit transitions can also be defined for all states (`AddExitTransitionFromAny`), as trigger transitions (`AddExitTriggerTransition`), or as both (`AddExitTriggerTransitionFromAny`).

- **Transition callbacks**: New feature that lets you define a function that is called when a transition succeeds. It is supported by all transition types (e.g. trigger transitions, transitions from any, exit transitions, ...).
  
  ```csharp
  fsm.AddTransition(
      new Transition("A", "B", onTransition: t => print("Transition"))
  );
  ```
  
  This feature is also supported when using the shortcut methods:
  
  ```csharp
  // Can be shortened using shortcut methods:
  fsm.AddTransition("A", "B", onTransition: t => print("Transition"));
  ```
  
  The print function will be called just before the transition. You can also define a callback that is called just after the transition:
  
  ```csharp
    fsm.AddTransition("A", "B",
      onTransition: t => print("Before"),
      afterTransition: t => print("After")
  );
  ```

- Support for **custom actions** in `HybridStateMachine`, just like in the normal `State` class:
  
  ```csharp
  var hybrid = new HybridStateMachine();
  hybrid.AddState("A", new State().AddAction("Action", () => print("A")));
  hybrid.AddAction("Action", () => print("Hybrid"));
  
  hybrid.Init();
  hybrid.OnAction("Action");  // Prints "Hybrid" and then "A"
  ```

- Option in `HybridStateMachine` to **run custom code before and after** the `OnEnter` / `OnLogic` / ... of its active sub state. Previously, you could only add a custom callback that was run *after* the respective methods of the sub state. When migrating to this version simply replace the `onEnter` parameter with `afterOnEnter` in the constructor. For example
  
  ```csharp
  var hybrid = new HybridStateMachine(
      beforeOnEnter: fsm => print("Before OnEnter"),
      afterOnLogic: fsm => print("After OnLogic")
      // ...
  )
  ```

- Feature for getting the **active path in the state hierarchy**: When debugging it is often useful to not only see what the active state of the root state machine is (using `ActiveStateName`) but also which state is active in any nested state machine. This path of states can now be retrieved using the new `GetActiveHierarchyPath()` method:
  
  ```csharp
  var fsm = new StateMachine();
  var move = new StateMachine();
  var jump = new StateMachine();
  
  fsm.AddState("Move", move);
  move.AddState("Jump", jump);
  jump.AddState("Falling");
  
  fsm.Init();
  print(fsm.GetActiveHierarchyPath());  // Prints "/Move/Jump/Falling"
  ```

- Option in `CoState` to **only run the coroutine once**. E.g.
  
  ```csharp
  var state = new CoState(mono, myCoroutine, loop: false);
  ```

- Option in `TransitionAfterDynamic` to only evaluate the dynamic delay when the `from` state enters. This is useful, e.g. when the delay of a transition should be random. E.g.
  
  ```csharp
  fsm.AddTransition(new TransitionAfterDynamic(
      "A", "B", t => Random.Range(2, 10), onlyEvaluateDelayOnEnter: true
  ));
  ```

### Improved

- `canExit` feature in `State` and `CoState`: The custom `canExit` function that determines when the state is ready to exit to allow for another transition is now called on every frame when a transition is pending and not only `OnExitRequest`. This is more intuitive and can therefore prevent some unexpected behaviour from emerging.

- The constructor of the `CoState` class now also allows you to pass in an IEnumerator function, that does not take the `CoState` as a parameter, as the coroutine

- More documentation for classes / parameters / ... directly visible in the IDE

- Internal refactors making the code easier to understand and read

### Changed

- **Important:** The namespace of UnityHFSM has changed from `FSM` to `UnityHFSM`. This means that you have to use `using UnityHFSM` now.
- The parameters `onEnter`, `onLogic`, ... in the constructor of the `HybridStateMachine` class are now equivalent to the new parameters `afterOnEnter`, `afterOnLogic`, ...
- The `onLogic` parameter in the constructor of the `CoState` class is now called `coroutine`, is the second parameter, and no longer optional.

### Fixed

- Multiple bugs relating to delayed transitions (pending transitions system)

---

## 1.9

### Added

- Action system to allow for adding and calling custom functions apart from `OnLogic`.
  
  E.g.
  
  ```csharp
  var state = new State()
    .AddAction("OnGameOver", () => print("Good game"))
    .AddAction<Collision2D>("OnCollision", collision => print(collision));
  
  fsm.AddState("State", state);
  fsm.Init();
  
  fsm.OnAction("OnGameOver");  // prints "Good game"
  fsm.OnAction<Collision2D>("OnCollision", new Collision2D());
  ```

- Two way transitions: New feature that lets the state machine transition from a source to a target state when a condition is true, and from the target to the source state when the condition is false:
  
  ```csharp
  fsm.AddTwoWayTransition("Idle", "Shoot", t => isInRange);
  
  // Same as
  fsm.AddTransition("Idle", "Shoot", t => isInRange);
  fsm.AddTransition("Shoot", "Idle", t => ! isInRange);
  ```
  
  ```csharp
  fsm.AddTwoWayTransition(transition);
  fsm.AddTwoWayTriggerTransition(transition);
  ```

- `TransitionOnMouse` classes for readable transitions that should occur when a certain mouse button has been pressed / released / ... It is analogous to `TransitionOnKey`.
  
  E.g.:
  
  ```csharp
  fsm.AddTransition(new TransitionOnMouse.Down("Idle", "Shoot", 0));
  ```

### Improved

- Improved performance in many cases for value types as the state names (e.g. `State<int>`) by preventing boxing and minimising GC allocations

### Changed

- The `RequestExit()` method of the StateBase class has been renamed to `OnExitRequest()` for more clarity.

- The "shortcut methods" of the state machine have been moved to a dedicated class as extension methods. This does not change the API or usage in any way, but makes the internal code cleaner. -> This change reduces the coupling between the base StateMachine class and the State / Transition classes. Instead, the StateMachine only depends on the StateBase and TransitionBase classes. This especially shows that the extension methods are optional and not necessary in a fundamental way.

- To allow for better testing and more customisation, references to the Timer class have been replaced with the ITimer interface. This allows you to write a custom timer for your use case and allows for time-based transitions to be tested more easily.
  
  ```csharp
  // Previously
  if (timer > 2) { }
  
  // Now
  if (timer.Elapsed > 2) { }
  ```

- As a consequence of the way the action system was implemented, generic datatype of the input parameter of `onEnter` / `onLogic` / `onExit` for `State` and `CoState` has changed. The class `State` now requires two generic type parameters: One for the type of its ID and one for the type of the IDs of the actions.
  
  Previously:
  
  ```csharp
  void FollowPlayer(State<string> state)
  {
      // ...
  }
  
  fsm.AddState("FollowPlayer", onLogic: FollowPlayer);
  ```
  
  Now:
  
  ```csharp
  void FollowPlayer(State<string, string> state)
  {
      // ...
  }
  
  fsm.AddState("FollowPlayer", onLogic: FollowPlayer);
  ```

- (Internal change) Restructured the `src` folder to make it cleaner

### Fixed

- Fix ArgumentNullException when using the `AddTransitionFromAny` shortcut method

---

## 1.8 - Generics

Version 1.8 of UnityHFSM adds support for generics. Now the datatype of state identifiers / names and the type of event names can be easily changed. Thanks to the new "shortcut methods", state machines can be written with less boilerplate than ever and certain cases, such as empty states, can be optimised automatically for you.

### Added

- Support for generics for the state identifiers and event names

- "Shortcut methods" for reduced boilerplate and automatic optimisation
  
  ```csharp
  fsm.AddState("FollowPlayer", new State(
      onLogic: s => MoveTowardsPlayer()
  ));
  // Now
  fsm.AddState("FollowPlayer", onLogic: s => MoveTowardsPlayer());
  ```
  
  ```csharp
  fsm.AddState("ExtractIntel", new State());
  // Now
  fsm.AddState("ExtractIntel");
  ```
  
  ```csharp
  fsm.AddTransition(new Transition("A", "B"));
  // Now
  fsm.AddTransition("A", "B");
  ```

- Support for installing the package via Unity's Package Manager UPM

- Project samples

### Changed

- The datatype of the input parameter of `onEnter` / `onLogic` / `onExit` for `State` has changed. This is due to the inheritance hierarchy and the way generic support was added to the codebase while still trying to retain the ease of use of the string versions.
  
  Previously:
  
  ```csharp
  void FollowPlayer(State state)
  {
      // ...
  }
  
  fsm.AddState("FollowPlayer", new State(onLogic: FollowPlayer));
  ```
  
  Now:
  
  ```csharp
  void FollowPlayer(State<string> state)
  {
      // ...
  }
  
  fsm.AddState("FollowPlayer", new State(onLogic: FollowPlayer));
  ```

- States and transitions no longer carry a reference to the MonoBehaviour by default.
  
  - Now the constructor of `StateMachine` does not require mono anymore => `new StateMachine()`  instead of `new StateMachine(this)`
  
  - The reference to mono has to be passed into the `CoState` constructor => `new CoState(this, ...)`

### Fixed

- Fix `KeyNotFoundException` being thrown when an event is activated while no active trigger transition use this event

- Fix incorrect order of events on state changes, which called the `OnEnter` method before the new active transitions / trigger transitions had been loaded

---
