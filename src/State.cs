using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
	public class State : StateBase {
		private Action<State> onEnter;
		private Action<State> onLogic;
		private Action<State> onExit;
		private Func<State, bool> canExit;

		public Timer timer;
		
		/// <summary>
		/// Initialises a new instance of the State class
		/// </summary>
		/// <param name="onEnter">A function that is called when the state machine enters this state</param>
		/// <param name="onLogic">A function that is called by the logic function of the state machine if this state is active</param>
		/// <param name="onExit">A function that is called when the state machine exits this state</param>
		/// <param name="canExit">(Only if needsExitTime is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, later in OnLogic() it should call fsm.StateCanExit()</param>
		/// <param name="needsExitTime">Determins if the state is allowed to instantly
		/// 	exit on a transition (false), or if the state machine should wait until the state is ready for a
		/// 	state change (true)</param>
		public State(
				Action<State> onEnter = null, 
				Action<State> onLogic = null,
				Action<State> onExit = null,
				Func<State, bool> canExit = null,
				bool needsExitTime = false) : base(needsExitTime) 
		{
			this.onEnter = onEnter;
			this.onLogic = onLogic;
			this.onExit = onExit;
			this.canExit = canExit;

			this.timer = new Timer();
		}

		override public void OnEnter() {
			timer.Reset();

			if (onEnter != null) onEnter(this);
		}

		override public void OnLogic() {
			if (onLogic != null) onLogic(this);
		}

		override public void OnExit() {
			if (onExit != null)	onExit(this);
		}

		override public void RequestExit() {
			if (!needsExitTime || canExit != null && canExit(this)) {
				fsm.StateCanExit();
			}
		}
	}
}
