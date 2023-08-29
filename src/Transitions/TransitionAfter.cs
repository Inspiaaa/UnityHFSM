using System;

namespace UnityHFSM
{
	/// <summary>
	/// A class used to determine whether the state machine should transition to another state
	/// depending on a delay and an optional condition.
	/// </summary>
	public class TransitionAfter<TStateId> : TransitionBase<TStateId>
	{
		public float delay;
		public ITimer timer;

		public Func<TransitionAfter<TStateId>, bool> condition;

		public Action<TransitionAfter<TStateId>> beforeTransition;
		public Action<TransitionAfter<TStateId>> afterTransition;

		/// <summary>
		/// Initialises a new instance of the TransitionAfter class.
		/// </summary>
		/// <param name="delay">The delay that must elapse before the transition can occur</param>
		/// <param name="condition">A function that returns true if the state machine
		/// 	should transition to the <c>to</c> state.
		/// 	It is only called after the delay has elapsed and is optional.</param>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
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

	/// <inheritdoc />
	public class TransitionAfter : TransitionAfter<string>
	{
		/// <inheritdoc />
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
