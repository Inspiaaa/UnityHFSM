using FSM;
using UnityEngine;

class CustomSendData : StateBase
{
    // Important: The constructor must call StateBase's constructor (here: base(...))
    // because it declares whether the state needsExitTime
    public CustomSendData() : base(needsExitTime: false)
    {
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
        this.mono.transform.eulerAngles += new Vector3(0, 0, 100 * Time.deltaTime);
    }
}