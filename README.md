# Finite State Machine for Unity

![](https://img.shields.io/badge/Unity-C%23-blue?style=for-the-badge&logo=unity)

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
        fsm = new StateMachine(gameObject);
    }
}
```

### Adding states
```csharp
    void Start()
    {
        fsm = new StateMachine(gameObject);
        
        fsm.AddState("ExtractIntel", new State());    // Empty state without any logic
	
	fsm.AddState("FollowPlayer", new State(
            onLogic: (state) => {
	        // Or however you have your scene and player configured
                Vector3 player = PlayerController.Instance.transform.position;
                
                // Move towards the player at 1 unit per second
                transform.position += (player - transform.position).normalized * Time.deltaTime;
            }
        ));
        
        fsm.AddState("FleeFromPlayer", new State(
            onLogic: (state) => {
                Vector3 player = PlayerController.Instance.transform.position;
                
                // Move away from player at 1 unit per second
                transform.position -= (player - transform.position).normalized * Time.deltaTime;
            }
        ));
	
        fsm.SetStartState("FollowPlayer");
    }
```

### Adding transitions

```csharp
    void Start()
    {
        // ...
        fsm.AddTransition(new Transition("ExtractIntel", "FollowPlayer",
            (transition) => {
                Vector3 player = PlayerController.Instance.transform.position;
                float distance = (player - transform.position).magnitude;
                return distance > ownScanningRange;
            }
        ));

        fsm.AddTransition(new Transition("FollowPlayer", "ExtractIntel",
            (transition) => {
                Vector3 player = PlayerController.Instance.transform.position;
                float distance = (player - transform.position).magnitude;
                return distance < ownScanningRange;
            }
        ));

        fsm.AddTransition(new Transition("ExtractIntel", "FleeFromPlayer",
            (transition) => {
                Vector3 player = PlayerController.Instance.transform.position;
                float distance = (player - transform.position).magnitude;
                return distance < playerScanningRange;
            }
        ));

        fsm.AddTransition(new Transition(
            "FleeFromPlayer",
            "ExtractIntel",
            (transition) => {
                Vector3 player = PlayerController.Instance.transform.position;
                float distance = (player - transform.position).magnitude;
                return distance > playerScanningRange;
            }
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
