using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FSM {
	public class FSMTransition {
		public string from;
		public string to;

		public bool forceInstantly;

		public StateMachine fsm;
		public GameObject gameObject;

		public FSMTransition(string from, string to, bool forceInstantly = false) 
		{
			this.from = from;
			this.to = to;
			this.forceInstantly = forceInstantly;
		}

		public virtual bool ShouldTransition() {
			return true;
		}
	}
}