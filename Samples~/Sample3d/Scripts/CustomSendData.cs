using UnityEngine;
using UnityHFSM; // Import the required classes for the state machine

namespace UnityHFSM.Samples.Sample3d
{
    class CustomSendData : StateBase
    {
        MonoBehaviour mono;

        // Important: The constructor must call StateBase's constructor (here: base(...))
        // because it declares whether the state needsExitTime
        public CustomSendData(MonoBehaviour mono) : base(needsExitTime: false)
        {
            // We need to have access to the MonoBehaviour so that we can rotate it.
            // => Keep a reference
            this.mono = mono;
        }

        public override void OnEnter()
        {
            // Write your code for OnEnter here
            // If you don't have any, you can just leave this entire method override out
        }

        public override void OnLogic()
        {
            this.mono.transform.eulerAngles += new Vector3(0, 0, 100 * Time.deltaTime);
        }
    }
}