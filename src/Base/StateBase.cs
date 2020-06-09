using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FSM {
	/// <summary>
	/// The base class of all states
	/// </summary>
	public class StateBase {
		public bool needsExitTime;
		public string name;

		public StateMachine fsm;
		public MonoBehaviour mono;

		/// <summary>
		/// Initialises a new instance of the BaseState class
		/// </summary>
		/// <param name="needsExitTime">Determins if the state is allowed to instantly
		/// 	exit on a transition (false), or if the state machine should wait until
		/// 	the state is ready for a state change (true)</param>
		public StateBase(bool needsExitTime) {
			this.needsExitTime = needsExitTime;
		}

		/// <summary>
		/// Called when the state machine transitions to this state (enters this state)
		/// </summary>
		public virtual void OnEnter() {

		}

		/// <summary>
		/// Called while this state is active
		/// </summary>
		public virtual void OnLogic() {

		}

		/// <summary>
		/// Called when the state machine transitions from this state to another state (exits this state)
		/// </summary>
		public virtual void OnExit() {

		}

		/// <summary>
		/// (Only if needsExitTime is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, it should call fsm.StateCanExit() later in OnLogic()
		/// </summary>
		public virtual void RequestExit() {
			
		}
	}
}
