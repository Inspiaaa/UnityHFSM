using System;

namespace UnityHFSM
{
	/// <summary>
	/// A class that allows you to run additional functions (companion code)
	/// before and after the wrapped state's code.
	/// </summary>
	public class TransitionWrapper<TStateId>
	{
		public class WrappedTransition : TransitionBase<TStateId>
		{
			private Action<TransitionBase<TStateId>>
				beforeOnEnter,
				afterOnEnter,

				beforeShouldTransition,
				afterShouldTransition;

			private TransitionBase<TStateId> transition;

			public WrappedTransition(
					TransitionBase<TStateId> transition,

					Action<TransitionBase<TStateId>> beforeOnEnter = null,
					Action<TransitionBase<TStateId>> afterOnEnter = null,

					Action<TransitionBase<TStateId>> beforeShouldTransition = null,
					Action<TransitionBase<TStateId>> afterShouldTransition = null) : base(
					transition.from, transition.to, forceInstantly: transition.forceInstantly)
			{
				this.transition = transition;

				this.beforeOnEnter = beforeOnEnter;
				this.afterOnEnter = afterOnEnter;

				this.beforeShouldTransition = beforeShouldTransition;
				this.afterShouldTransition = afterShouldTransition;
			}

			public override void Init()
			{
				transition.fsm = this.fsm;
			}

			public override void OnEnter()
			{
				beforeOnEnter?.Invoke(transition);
				transition.OnEnter();
				afterOnEnter?.Invoke(transition);
			}

			public override bool ShouldTransition()
			{
				beforeShouldTransition?.Invoke(transition);
				bool shouldTransition = transition.ShouldTransition();
				afterShouldTransition?.Invoke(transition);
				return shouldTransition;
			}

			public override void BeforeTransition()
			{
				transition.BeforeTransition();
			}

			public override void AfterTransition()
			{
				transition.AfterTransition();
			}
		}

		private Action<TransitionBase<TStateId>>
			beforeOnEnter,
			afterOnEnter,

			beforeShouldTransition,
			afterShouldTransition;

		public TransitionWrapper(
				Action<TransitionBase<TStateId>> beforeOnEnter = null,
				Action<TransitionBase<TStateId>> afterOnEnter = null,

				Action<TransitionBase<TStateId>> beforeShouldTransition = null,
				Action<TransitionBase<TStateId>> afterShouldTransition = null)
		{
			this.beforeOnEnter = beforeOnEnter;
			this.afterOnEnter = afterOnEnter;

			this.beforeShouldTransition = beforeShouldTransition;
			this.afterShouldTransition = afterShouldTransition;
		}

		public WrappedTransition Wrap(TransitionBase<TStateId> transition)
		{
			return new WrappedTransition(
				transition,
				beforeOnEnter,
				afterOnEnter,
				beforeShouldTransition,
				afterShouldTransition
			);
		}
	}

	/// <inheritdoc />
	public class TransitionWrapper : TransitionWrapper<string>
	{
		public TransitionWrapper(
			Action<TransitionBase<string>> beforeOnEnter = null,
			Action<TransitionBase<string>> afterOnEnter = null,

			Action<TransitionBase<string>> beforeShouldTransition = null,
			Action<TransitionBase<string>> afterShouldTransition = null) : base(
			beforeOnEnter, afterOnEnter,
			beforeShouldTransition, afterShouldTransition)
		{
		}
	}
}
