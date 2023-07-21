using System;

namespace FSM
{
	/// <summary>
	/// A class used to determine whether the state machine should transition to another state
	/// depending on a delay and an optional condition
	/// </summary>
	public class TransitionAfter<TStateId> : TransitionBase<TStateId>
	{

		public float delay;
		public ITimer timer;

		public Func<TransitionAfter<TStateId>, bool> condition;

		public Action<TransitionAfter<TStateId>> beforeTransition;
		public Action<TransitionAfter<TStateId>> afterTransition;


		/// <summary>
		/// Initialises a new instance of the TransitionAfter class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="delay">The delay that must elapse before the transition can occur</param>
		/// <param name="condition">A function that returns true if the state machine
		/// 	should transition to the <c>to</c> state.
		/// 	It is only called after the delay has elapsed and is optional.</param>
		/// <param name="onTransition">Callback function that is called just before the transition happens.</param>
		/// <param name="afterTransition">Callback function that is called just after the transition happens.</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true
		/// 	=> Forces an instant transition</param>
		public TransitionAfter(
				TStateId from,
				TStateId to,
				float delay,
				Func<TransitionAfter<TStateId>, bool> condition = null,
				Action<TransitionAfter<TStateId>> onTransition = null,
				Action<TransitionAfter<TStateId>> afterTransition = null,
				bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.delay = delay;
			this.condition = condition;
			this.beforeTransition = onTransition;
			this.afterTransition = afterTransition;
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

		public override void BeforeTransition() => beforeTransition?.Invoke(this);
		public override void AfterTransition() => afterTransition?.Invoke(this);
	}

	public class TransitionAfter : TransitionAfter<string>
	{
		public TransitionAfter(
			string @from,
			string to,
			float delay,
			Func<TransitionAfter<string>, bool> condition = null,
			Action<TransitionAfter<string>> onTransition = null,
			Action<TransitionAfter<string>> afterTransition = null,
			bool forceInstantly = false) : base(
				@from,
				to,
				delay,
				condition,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly)
		{
		}
	}
}
