using System;

namespace UnityHFSM
{
	/// <summary>
	/// A class used to determine whether the state machine should transition to another state
	/// depending on a dynamically computed delay and an optional condition.
	/// </summary>
	public class TransitionAfterDynamic<TStateId> : TransitionBase<TStateId>
	{
		public ITimer timer;
		private float delay;
		private readonly bool onlyEvaluateDelayOnEnter;
		private readonly Func<TransitionAfterDynamic<TStateId>, float> delayCalculator;

		private readonly Func<TransitionAfterDynamic<TStateId>, bool> condition;

		private readonly Action<TransitionAfterDynamic<TStateId>> beforeTransition;
		private readonly Action<TransitionAfterDynamic<TStateId>> afterTransition;

		/// <summary>
		/// Initialises a new instance of the TransitionAfterDynamic class.
		/// </summary>
		/// <param name="delay">A function that dynamically computes the delay time.</param>
		/// <param name="condition">A function that returns true if the state machine
		/// 	should transition to the <c>to</c> state.
		/// 	It is only called after the delay has elapsed and is optional.</param>
		/// <param name="onlyEvaluateDelayOnEnter">If true, the dynamic delay is only recalculated
		/// 	when the <c>from</c> enters. If false, the delay is evaluated in each logic step.</param>
		/// <inheritdoc cref="Transition{TStateId}(TStateId, TStateId, Func{Transition{TStateId}, bool},
		/// 	Action{Transition{TStateId}}, Action{Transition{TStateId}}, bool)" />
		public TransitionAfterDynamic(
				TStateId from,
				TStateId to,
				Func<TransitionAfterDynamic<TStateId>, float> delay,
				Func<TransitionAfterDynamic<TStateId>, bool> condition = null,
				bool onlyEvaluateDelayOnEnter = false,
				Action<TransitionAfterDynamic<TStateId>> onTransition = null,
				Action<TransitionAfterDynamic<TStateId>> afterTransition = null,
				bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.delayCalculator = delay;
			this.condition = condition;
			this.onlyEvaluateDelayOnEnter = onlyEvaluateDelayOnEnter;
			this.beforeTransition = onTransition;
			this.afterTransition = afterTransition;
			this.timer = new Timer();
		}

		public override void OnEnter()
		{
			timer.Reset();
			if (onlyEvaluateDelayOnEnter)
			{
				delay = delayCalculator(this);
			}
		}

		public override bool ShouldTransition()
		{
			if (!onlyEvaluateDelayOnEnter)
			{
				delay = delayCalculator(this);
			}

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
	public class TransitionAfterDynamic : TransitionAfterDynamic<string>
	{
		/// <inheritdoc />
		public TransitionAfterDynamic(
			string @from,
			string to,
			Func<TransitionAfterDynamic<string>, float> delay,
			Func<TransitionAfterDynamic<string>, bool> condition = null,
			bool onlyEvaluateDelayOnEnter = false,
			Action<TransitionAfterDynamic<string>> onTransition = null,
			Action<TransitionAfterDynamic<string>> afterTransition = null,
			bool forceInstantly = false) : base(
				@from,
				to,
				delay,
				condition,
				onlyEvaluateDelayOnEnter: onlyEvaluateDelayOnEnter,
				onTransition: onTransition,
				afterTransition: afterTransition,
				forceInstantly: forceInstantly)
		{
		}
	}
}
