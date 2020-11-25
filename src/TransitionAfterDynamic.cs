using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
	/// <summary>
	/// A class used to determin whether the state machine should transition to another state
	/// depending on a dynamically computed delay and an optional condition
	/// </summary>
	public class TransitionAfterDynamic : TransitionBase{

		public Func<TransitionAfterDynamic, float> delayCalculator;
		public Func<TransitionAfterDynamic, bool> condition;
		public Timer timer;

		/// <summary>
		/// Initialises a new instance of the TransitionAfterDynamic class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="delay">A function that dynamically computes the delay time</param>
		/// <param name="condition">A function that returns true if the state machine 
		/// 	should transition to the <c>to</c> state. 
		/// 	It is only called after the delay has elapsed and is optional.</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true 
		/// 	=> Forces an instant transition</param>
		public TransitionAfterDynamic(
				string from, 
				string to, 
				Func<TransitionAfterDynamic, float> delay,
				Func<TransitionAfterDynamic, bool> condition = null,
				bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.delayCalculator = delay;
			this.condition = condition;
			this.timer = new Timer();
		}

		public override void OnEnter() {
			timer.Reset();
		}

		public override bool ShouldTransition() {
			if (timer < delayCalculator(this))
				return false;

			if (condition == null)
				return true;
			
			return condition(this);
		}
	}
}
	