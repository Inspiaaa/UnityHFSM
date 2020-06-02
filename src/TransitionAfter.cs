using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
	/// <summary>
	/// A class used to determin whether the state machine should transition to another state
	/// </summary>
	public class TransitionAfter : FSMTransition{

		public float delay;
		public Func<TransitionAfter, bool> condition;
		public Timer timer;

		/// <summary>
		/// Initialises a new instance of the Transition class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="delay">The name / identifier of the next state</param>
		/// <param name="condition">A function that returns true if the state machine should transition to the <c>to</c> state. 
		/// 						It is only called once the delay has elapsed and is optional.</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true => Forces an instant transition</param>
		public TransitionAfter(string from, 
				string to, 
				float delay,
				Func<TransitionAfter, bool> condition = null,
				bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.delay = delay;
			this.condition = condition;
			this.timer = new Timer();
		}

		public override void OnEnter() {
			timer.Reset();
		}

		public override bool ShouldTransition() {
			if (timer < delay)
				return false;

			if (condition == null)
				return true;
			
			return condition(this);
		}
	}
}