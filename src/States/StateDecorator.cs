using System;

namespace UnityHFSM
{
	/// <summary>
	/// A helper class that helps you decorate multiple states with the same user code.
	/// It produces <see cref="DecoratedState"/> objects based on the provided parameters.
	/// </summary>
	public class StateDecorator<TStateId, TEvent>
	{
		private readonly Action<StateBase<TStateId>>
			beforeOnEnter,
			afterOnEnter,

			beforeOnLogic,
			afterOnLogic,

			beforeOnExit,
			afterOnExit;

		/// <summary>
		/// Initialises a new instance of the StateDecorator class.
		/// </summary>
		public StateDecorator(
				Action<StateBase<TStateId>> beforeOnEnter = null,
				Action<StateBase<TStateId>> afterOnEnter = null,

				Action<StateBase<TStateId>> beforeOnLogic = null,
				Action<StateBase<TStateId>> afterOnLogic = null,

				Action<StateBase<TStateId>> beforeOnExit = null,
				Action<StateBase<TStateId>> afterOnExit = null)
		{
			this.beforeOnEnter = beforeOnEnter;
			this.afterOnEnter = afterOnEnter;

			this.beforeOnLogic = beforeOnLogic;
			this.afterOnLogic = afterOnLogic;

			this.beforeOnExit = beforeOnExit;
			this.afterOnExit = afterOnExit;
		}

		public DecoratedState<TStateId, TEvent> Decorate(StateBase<TStateId> state)
		{
			return new DecoratedState<TStateId, TEvent>(
				state,
				beforeOnEnter,
				afterOnEnter,
				beforeOnLogic,
				afterOnLogic,
				beforeOnExit,
				afterOnExit
			);
		}
	}

	/// <inheritdoc />
	public class StateDecorator : StateDecorator<string, string>
	{
		public StateDecorator(
			Action<StateBase<string>> beforeOnEnter = null,
			Action<StateBase<string>> afterOnEnter = null,

			Action<StateBase<string>> beforeOnLogic = null,
			Action<StateBase<string>> afterOnLogic = null,

			Action<StateBase<string>> beforeOnExit = null,
			Action<StateBase<string>> afterOnExit = null) : base(
			beforeOnEnter, afterOnEnter,
			beforeOnLogic, afterOnLogic,
			beforeOnExit, afterOnExit)
		{
		}
	}
}
