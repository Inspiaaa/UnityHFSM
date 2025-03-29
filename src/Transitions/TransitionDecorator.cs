using System;

namespace UnityHFSM
{
	/// <summary>
	/// A helper class that helps you decorate multiple transitions with the same user code.
	/// It produces <see cref="DecoratedTransition"/> objects based on the provided parameters.
	/// </summary>
	public class TransitionDecorator<TStateId>
	{
		private readonly Action<TransitionBase<TStateId>>
			beforeOnEnter,
			afterOnEnter,

			beforeShouldTransition,
			afterShouldTransition,

			beforeOnTransition,
			afterOnTransition,

			beforeAfterTransition,
			afterAfterTransition;

		public TransitionDecorator(
			Action<TransitionBase<TStateId>> beforeOnEnter = null,
			Action<TransitionBase<TStateId>> afterOnEnter = null,

			Action<TransitionBase<TStateId>> beforeShouldTransition = null,
			Action<TransitionBase<TStateId>> afterShouldTransition = null,

			Action<TransitionBase<TStateId>> beforeOnTransition = null,
			Action<TransitionBase<TStateId>> afterOnTransition = null,

			Action<TransitionBase<TStateId>> beforeAfterTransition = null,
			Action<TransitionBase<TStateId>> afterAfterTransition = null)
		{
			this.beforeOnEnter = beforeOnEnter;
			this.afterOnEnter = afterOnEnter;

			this.beforeShouldTransition = beforeShouldTransition;
			this.afterShouldTransition = afterShouldTransition;

			this.beforeOnTransition = beforeOnTransition;
			this.afterOnTransition = afterOnTransition;

			this.beforeAfterTransition = beforeAfterTransition;
			this.afterAfterTransition = afterAfterTransition;
		}

		public DecoratedTransition<TStateId> Decorate(TransitionBase<TStateId> transition)
		{
			return new DecoratedTransition<TStateId>(
				transition,
				beforeOnEnter,
				afterOnEnter,
				beforeShouldTransition,
				afterShouldTransition,
				beforeOnTransition,
				afterOnTransition,
				beforeAfterTransition,
				afterAfterTransition
			);
		}
	}

	/// <inheritdoc />
	public class TransitionDecorator : TransitionDecorator<string>
	{
		public TransitionDecorator(
			Action<TransitionBase<string>> beforeOnEnter = null,
			Action<TransitionBase<string>> afterOnEnter = null,
			Action<TransitionBase<string>> beforeShouldTransition = null,
			Action<TransitionBase<string>> afterShouldTransition = null,
			Action<TransitionBase<string>> beforeOnTransition = null,
			Action<TransitionBase<string>> afterOnTransition = null,
			Action<TransitionBase<string>> beforeAfterTransition = null,
			Action<TransitionBase<string>> afterAfterTransition = null)
		: base(
			beforeOnEnter,
			afterOnEnter,
			beforeShouldTransition,
			afterShouldTransition,
			beforeOnTransition,
			afterOnTransition,
			beforeAfterTransition,
			afterAfterTransition) { }
	}
}
