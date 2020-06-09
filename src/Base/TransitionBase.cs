using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FSM {
	/// <summary>
	/// The base class of all transitions
	/// </summary>
	public class TransitionBase {
		public string from;
		public string to;

		public bool forceInstantly;

		public StateMachine fsm;
		public MonoBehaviour mono;

		/// <summary>
		/// Initialises a new instance of the TransitionBase class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true 
		/// 	=> Forces an instant transition</param>
		public TransitionBase(string from, string to, bool forceInstantly = false) 
		{
			this.from = from;
			this.to = to;
			this.forceInstantly = forceInstantly;
		}

		/// <summary>
		/// Called when the state machine enters the "from" state
		/// </summary>
		public virtual void OnEnter() { }

		/// <summary>
		/// Called to determin whether the state machine should transition to the <c>to</c> state
		/// </summary>
		/// <returns>True if the state machine should change states / transition</returns>
		public virtual bool ShouldTransition() {
			return true;
		}
	}
}
