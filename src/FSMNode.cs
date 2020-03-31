using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FSM {
	public class FSMNode {
		public bool needsExitTime;
		public string name;

		public StateMachine fsm;
		public GameObject gameObject;

		public FSMNode(bool needsExitTime) {
			this.needsExitTime = needsExitTime;
		}

		public virtual void OnEnter() {

		}

		public virtual void OnLogic() {

		}

		public virtual void OnExit() {

		}

		public virtual void RequestExit() {
			
		}
	}
}