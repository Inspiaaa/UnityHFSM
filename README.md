# Finite State Machine for Unity

![](https://img.shields.io/badge/Unity3D-C%23-blue.svg?style=for-the-badge&logo=unity)

A simple yet powerful **hierarchical finite state machine** for the Unity game engine. It is scalable and customisable by being **class-based**, but also supports functions (or lambdas) for **fast prototyping**.

- [Fast prototyping](#simple-state-machine)

- [Hierarchical features](#hierarchical-state-machine)

- [Multiple state change patterns](#state-change-patterns)

- [Unity **coroutines**](#unity-coroutines)

- [Scalable (class-based)](#class-based-architecture)

## Examples

## Simple State Machine

Here's a simple state machine for an enemy spy in your game.

![](https://raw.githubusercontent.com/LavaAfterburner/UnityHFSM/master/diagrams/EnemySpyExample.png)

As you can see the enemy will try to stay outside of the player's scanning range while extracting intel. When the player goes too far away, it will follow the player again.

### The idea:

- Initialise the state machine

- Add states:
  
  ```csharp
  fsm.AddState( new State(
      onEnter,
      onLogic,
      onExit
  ));
  ```

- Add transitions
  
  ```csharp
  fsm.AddTransition( new Transition(
      from,
      to,
      condition
  ));
  ```

- Run the state machine
  
  ```csharp
  void Update {
      fsm.OnLogic()
  }
  ```

#### Initialising the state machine

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSM;    // Import the required classes for the state machine

public class EnemyController : MonoBehaviour
{
    private StateMachine fsm;
    public float playerScanningRange = 4f;
    public float ownScanningRange = 6f;

    void Start()
    {
        fsm = new StateMachine(this);
    }
}
```

#### Adding states

```csharp
    float DistanceToPlayer() {
        // Or however you have your scene and player configured
        return (transform.position - PlayerController.Instance.transform.position).magnitude;
    }

    void MoveTowardsPlayer(float speed) {
        // Or however you have your scene and player configured
        Vector3 player = PlayerController.Instance.transform.position;
        transform.position += (player - transform.position).normalized * speed * Time.deltaTime;
    }

    void Start()
    {
        fsm = new StateMachine(this);

        // Empty state without any logic
        fsm.AddState("ExtractIntel", new State());

        fsm.AddState("FollowPlayer", new State(
            onLogic: (state) => MoveTowardsPlayer(1)
        ));

        fsm.AddState("FleeFromPlayer", new State(
            onLogic: (state) => MoveTowardsPlayer(-1)
        ));

        // This configures the entry point of the state machine
        fsm.SetStartState("FollowPlayer");
        // Initialises the state machine and must be called before OnLogic() is called
        fsm.OnEnter()
    }
```

Although this example is using lambda expressions for the states' logic, you can of course just pass normal functions.

# 

#### Adding transitions

```csharp
    void Start()
    {
        // ...

        fsm.AddTransition(new Transition(
            "ExtractIntel", 
            "FollowPlayer",
            (transition) => DistanceToPlayer() > ownScanningRange
        ));

        fsm.AddTransition(new Transition(
            "FollowPlayer", 
            "ExtractIntel",
            (transition) => DistanceToPlayer() < ownScanningRange
        ));

        fsm.AddTransition(new Transition(
            "ExtractIntel", 
            "FleeFromPlayer",
            (transition) => DistanceToPlayer() < playerScanningRange
        ));

        fsm.AddTransition(new Transition(
            "FleeFromPlayer",
            "ExtractIntel",
            (transition) => DistanceToPlayer() > playerScanningRange
        ));
    }
```

#### Initialising and running the state machine

```csharp
    void Start() 
    {
        // ...

        fsm.OnEnter();
    }

    void Update()
    {
        fsm.OnLogic();
    }
```

## Hierarchical State Machine

Because StateMachine inherits from StateBase, it can be treated as a normal state, therefore allowing for the nesting of state machines together with states.

### Expanding on the previous example

![](https://raw.githubusercontent.com/LavaAfterburner/UnityHFSM/master/diagrams/EnemySpyHierarchicalExample.png)

So that you can see a visual difference, the enemy should be spinning when it enters the "SendData" state.

### The idea:

- Create a separate state machine for the nested states (States in Extract Intel)

- Add the nested states to the new state machine

- Add the new state machine to the main state machine as a normal state

#### 

#### Separate FSM for the ExtractIntel state

```csharp
    void Start()
    {
        // This is the main state machine
        fsm = new StateMachine(this);

        // This is the nested state machine
        StateMachine extractIntel = new StateMachine(this, needsExitTime: false);
        fsm.AddState("ExtractIntel", extractIntel);

        // ...
    }
```

#### Adding States and Transitions

```csharp
    void Start()
    {
        fsm = new StateMachine(this);

        StateMachine extractIntel = new StateMachine(this, needsExitTime: false);
        fsm.AddState("ExtractIntel", extractIntel);

        extractIntel.AddState("SendData", new State(
            onLogic: (state) => {
                // When the state has been active for more than 5 seconds,
                // notify the fsm that the state can cleanly exit
                if (state.timer > 5)
                    state.fsm.StateCanExit();

                // Make the enemy turn at 100 degrees per second
                transform.rotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, 0, Time.deltaTime * 100));
            },
            // This means the state won't instantly exit when a transition should happen
            // but instead the state machine waits until it is given permission to change state
            needsExitTime: true
        ));

        extractIntel.AddState("CollectData", new State(
            onLogic: (state) => {if (state.timer > 5) state.fsm.StateCanExit();},
            needsExitTime: true
        ));

        // A transition without a condition
        extractIntel.AddTransition(new Transition("SendData", "CollectData"));
        extractIntel.AddTransition(new Transition("CollectData", "SendData"));
        extractIntel.SetStartState("CollectData");

        // ...
    }
```

What is `fsm.StateCanExit()` and `needsExitTime`?

When needsExitTime is set to false, the state can exit any time (because of a transition), regardless of its state (Get it? :) ).  If it is set to true this cannot happen (unless a transition has the `forceInstantly`  property). This is very useful when you do not want an action to be interrupted before it has ended, like in this case. 

But when is the right time for the state machine to finally change states? This is where the `fsm.StateCanExit()` method comes in and another argument for the State constructor: `canExit`.  `fsm.StateCanExit()` notifies the state machine that the state can cleanly exit.

1. When a transition should happen, the state machine calls `activeState.RequestExit()`, this calling the `canExit` function. If the state can exit, the `canExit` function has to call `fsm.StateCanExit()` and if not, it doesn't call `fsm.StateCanExit()`.

2. If the state couldn't exit when `canExit` was called, the active state has to notify the state machine at a later point in time, that it can exit, by calling the `fsm.StateCanExit()` method.

![](https://raw.githubusercontent.com/LavaAfterburner/UnityHFSM/master/diagrams/StateChangeFlowChart.png)

## State Change Patterns

The state machine supports two ways of changing states:

1. Using transitions as described earlier
   
   ```csharp
   fsm.AddTransition( new Transition(
       from,
       to,
       condition
   ));
   ```

2. Calling the `RequestStateChange` method
   
   ```csharp
   fsm.RequestStateChange(state, forceInstantly: false);
   ```
   
   **Example**
   
   ```csharp
   fsm.AddState("FollowPlayer", new State(
       onLogic: (state) => {
           MoveTowardsPlayer(1);
   
           if (DistanceToPlayer() < ownScanningRange)
               fsm.RequestStateChange("ExtractIntel")
       }));
   ```

## Unity Coroutines

By using the `CoState` class you can run coroutines. This class handles the following things automatically:

- Starting the Coroutine

- Running the Coroutine again once it has completed

- Terminating the Coroutine on state exit

As a result of a [limitation of the C# language](https://stackoverflow.com/questions/35473442/yield-return-in-the-lambda-expression), you can sadly not use lambda expressions to define IEnumerators (=> Coroutines).

In this example, we can replace the `SendData` state with a more advanced one, which makes the spy turn in one direction for two seconds, and the in the other direction for the same duration.

```csharp
    IEnumerator SendData(CoState state) {
        while (state.timer.Elapsed < 2) {
            transform.rotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, 0, Time.deltaTime * 100));
            // Wait until the next frame
            yield return null;
        }

        while (state.timer.Elapsed < 4) {
            transform.rotation = Quaternion.Euler(transform.eulerAngles - new Vector3(0, 0, Time.deltaTime * 100));
            yield return null;
        }

        state.timer.Reset();
        // Because needsExitTime is true, we have to tell the FSM when it can
        // safely exit the state
        state.fsm.StateCanExit();
        yield break;
    }

    void Start()
    {
        // ...
        extractIntel.AddState("SendData", new CoState(
            onLogic: SendData,
            needsExitTime: true
        ));
        // ...
    }
```

## Class based architecture

Because the states, transitions and the state machine itself are implemented in a object oriented manner, custom state and transition classes can be created. By inheriting from the common base classes (`StateBase`, `TransitionBase`), custom states and transitions can be developed.
This is also how `CoState`, `TransitionAfter`, ... have been implemented internally.

**Creating your own states**

Simply inherit from the base class `StateBase` and override the methods you need.

```csharp
    class CustomSendData : StateBase {
        // Important: The constructor must call StateBase's constructor (here: base(...))
        // because it declares whether the state needsExitTime
        public CustomSendData() : base(needsExitTime: false) {
            // Optional initialisation code here
        }

        public override void OnEnter()
        {
            // Write your code for OnEnter here
            // If you don't have any, you can just leave this entire method override out
        }

        public override void OnLogic()
        {
            // The MonoBehaviour can be accessed from inside the state with this.mono or simply mono
            this.mono.transform.rotation = Quaternion.Euler(
                this.mono.transform.eulerAngles + new Vector3(0, 0, Time.deltaTime * 100));
        }
    }

    void Start()
    {
        // ...
        extractIntel.AddState("SendData", new CustomSendData());
        // ...
    }
```

More documentation coming soon...
