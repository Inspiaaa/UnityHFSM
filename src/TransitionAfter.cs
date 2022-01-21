using System;

namespace FSM
{
	/// <summary>
	/// A class used to determin whether the state machine should transition to another state
	/// depending on a delay and an optional condition
	/// </summary>
	public class TransitionAfter<TStateId> : TransitionBase<TStateId>
	{

		public float delay;
		public Func<TransitionAfter<TStateId>, bool> condition;
		public ITimer timer;

		/// <summary>
		/// Initialises a new instance of the TransitionAfter class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="delay">The delay that must elapse before the transition can occur</param>
		/// <param name="condition">A function that returns true if the state machine
		/// 	should transition to the <c>to</c> state.
		/// 	It is only called after the delay has elapsed and is optional.</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true
		/// 	=> Forces an instant transition</param>
		public TransitionAfter(
				TStateId from,
				TStateId to,
				float delay,
				Func<TransitionAfter<TStateId>, bool> condition = null,
				bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.delay = delay;
			this.condition = condition;
			this.timer = new Timer();
		}

		public override void OnEnter()
		{
			timer.Reset();
		}

		public override bool ShouldTransition()
		{
			if (timer.Elapsed < delay)
				return false;

			if (condition == null)
				return true;

			return condition(this);
		}
	}

	public class TransitionAfter : TransitionAfter<string>
	{
		public TransitionAfter(
			string @from,
			string to,
			float delay,
			Func<TransitionAfter<string>, bool> condition = null,
			bool forceInstantly = false) : base(@from, to, delay, condition, forceInstantly)
		{
		}
	}
}
