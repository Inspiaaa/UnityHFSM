# Changelog

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
