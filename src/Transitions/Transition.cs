using System;

namespace FSM
{
	/// <summary>
	/// A class used to determine whether the state machine should transition to another state
	/// </summary>
	public class Transition<TStateId> : TransitionBase<TStateId>
	{

		public Func<Transition<TStateId>, bool> condition;
		public Action<Transition<TStateId>> beforeTransition;
		public Action<Transition<TStateId>> afterTransition;

		/// <summary>
		/// Initialises a new instance of the Transition class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="condition">A function that returns true if the state machine
		/// 	should transition to the <c>to</c> state</param>
		/// <param name="onTransition">Callback function that is called just before the transition happens.</param>
		/// <param name="afterTransition">Callback function that is called just after the transition happens.</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true
		/// 	=> Forces an instant transition</param>
		public Transition(
				TStateId from,
				TStateId to,
				Func<Transition<TStateId>, bool> condition = null,
				Action<Transition<TStateId>> onTransition = null,
				Action<Transition<TStateId>> afterTransition = null,
				bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.condition = condition;
			this.beforeTransition = onTransition;
			this.afterTransition = afterTransition;
		}

		public override bool ShouldTransition()
		{
			if (condition == null)
				return true;

			return condition(this);
		}

		public override void BeforeTransition() => beforeTransition?.Invoke(this);
		public override void AfterTransition() => afterTransition?.Invoke(this);
	}

	public class Transition : Transition<string>
	{
		public Transition(
			string @from,
			string to,
			Func<Transition<string>, bool> condition = null,
			Action<Transition<string>> onTransition = null,
			Action<Transition<string>> afterTransition = null,
			bool forceInstantly = false) : base(
				@from,
				to,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly)
		{
		}
	}
}
