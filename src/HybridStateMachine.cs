using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
    // A State-like StateMachine that allows you to run additional functions
    // especially handy for hierarchichal state machines
    // e.g. Jump and Move should both have the camera follow them
    // => Create a CodeStateMachine that has the camera work onLogic
    // As CodeStateMachine should run code that both Jump and Move should share,
    // the functions are run after running the active state's code.

    /// <summary>
    /// A State-like StateMachine that allows you to run additional functions (companion code)
    /// with the sub-states.
    /// It is especially handy for hierarchical state machines, as it allows you to factor out
    /// common code from the sub states into the HybridStateMachines, essentially removing
    /// duplicate code.
    /// </summary>
    public class HybridStateMachine : StateMachine
    {
        private Action<HybridStateMachine> onEnter;
		private Action<HybridStateMachine> onLogic;
		private Action<HybridStateMachine> onExit;

		public Timer timer;

        /// <summary>
        /// Initialises a new instance of the HybridStateMachine class
        /// </summary>
        /// <param name="mono">The MonoBehaviour of the script that created the state machine</param>
        /// <param name="onEnter">A function that is called after running the sub-state's OnEnter method
        /// when this state machine is entered</param>
        /// <param name="onLogic">A function that is called after running the sub-state's OnLogic method
        /// if this state machine is the active state</param>
        /// <param name="onExit">A function that is called after running the sub-state's OnExit method
        /// when this state machine is left</param>
        /// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determins whether the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until the active state is ready for a
		/// 	state change (true).</param>
        public HybridStateMachine(
                MonoBehaviour mono,
                Action<HybridStateMachine> onEnter = null, 
				Action<HybridStateMachine> onLogic = null,
				Action<HybridStateMachine> onExit = null,
                bool needsExitTime = false) : base(mono, needsExitTime)
        {
            this.onEnter = onEnter;
            this.onLogic = onLogic;
            this.onExit = onExit;

            this.timer = new Timer();
        }

		public override void OnEnter()
		{
			base.OnEnter();

            timer.Reset();
            onEnter?.Invoke(this);
		}

		public override void OnLogic()
		{
			base.OnLogic();

            onLogic?.Invoke(this);
		}

		public override void OnExit()
		{
			base.OnExit();

            onExit?.Invoke(this);
		}
		
    }
}