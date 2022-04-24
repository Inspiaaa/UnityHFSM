using System;

namespace FSM
{
	/// <summary>
	/// A class used to determin whether the state machine should transition to another state
	/// </summary>
	public class Transition<TStateId> : TransitionBase<TStateId>
	{

		public Func<Transition<TStateId>, bool> condition;

		/// <summary>
		/// Initialises a new instance of the Transition class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="condition">A function that returns true if the state machine
		/// 	should transition to the <c>to</c> state</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true
		/// 	=> Forces an instant transition</param>
		public Transition(
				TStateId from,
				TStateId to,
				Func<Transition<TStateId>, bool> condition = null,
				bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.condition = condition;
		}

		public override bool ShouldTransition()
		{
			if (condition == null)
				return true;

			return condition(this);
		}
	}

	public class Transition : Transition<string>
	{
		public Transition(
			string @from,
			string to,
			Func<Transition<string>, bool> condition = null,
			bool forceInstantly = false) : base(@from, to, condition, forceInstantly)
		{
		}
	}
}
