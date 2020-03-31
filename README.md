# Finite State Machine for Unity



A simple yet powerful finite state machine for the Unity game engine. It is class based, but also supports functions for fast prototyping.

## Example

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

- Run the statemachine
  
  ```csharp
  void Update {
   fsm.OnLogic()
  }
  ```

### Initialising the state machine

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FSM;    // This line imports the required classes for the state machine

public class EnemyController : MonoBehaviour
{
    private StateMachine fsm;
    public float playerScanningRange = 4f;
    public float ownScanningRange = 6f;

    void Start()
    {
        // Create a new StateMachine instance
        fsm = new StateMachine(gameObject);
    }
}
```

### Adding states

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
        fsm = new StateMachine(gameObject);

        // Empty state without any logic
        fsm.AddState("ExtractIntel", new State());

        fsm.AddState("FollowPlayer", new State(
            // Move towards player at 1 unit per second
            onLogic: (state) => MoveTowardsPlayer(1)
        ));

        fsm.AddState("FleeFromPlayer", new State(
            // Move away from player at 1 unit per second
            onLogic: (state) => MoveTowardsPlayer(-1)
        ));

        // This configures the entry point of the state machine
        fsm.SetStartState("FollowPlayer");
    }
```

### Adding transitions

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

### Initialising and runnning the state machine

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
