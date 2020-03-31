using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
	public class Transition {
		public string from;
		public string to;

		public Func<Transition, bool> condition;
		public bool forceInstantly;

		public StateMachine fsm;
		public GameObject gameObject;

		public Transition(string from, 
				string to, 
				Func<Transition, bool> condition = null,
				bool forceInstantly = false) 
		{
			this.from = from;
			this.to = to;
			this.condition = condition;
			this.forceInstantly = forceInstantly;
		}

		public bool ShouldTransition() {
			if (condition == null)
				return true;
			
			return condition(this);
		}
	}
}