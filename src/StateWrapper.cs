using System;

namespace FSM
{
	/// <summary>
	/// A class that allows you to run additional functions (companion code)
	/// before and after the wrapped state's code.
	/// It does not interfere with the wrapped state's timing / needsExitTime / ... behaviour.
	/// </summary>
	public class StateWrapper<TStateId, TEvent>
	{
		public class WrappedState : StateBase<TStateId>, ITriggerable<TEvent>, IActionable<TEvent>
		{
			private Action<StateBase<TStateId>>
				beforeOnEnter,
				afterOnEnter,

				beforeOnLogic,
				afterOnLogic,

				beforeOnExit,
				afterOnExit;

			private StateBase<TStateId> state;

			public WrappedState(
					StateBase<TStateId> state,

					Action<StateBase<TStateId>> beforeOnEnter = null,
					Action<StateBase<TStateId>> afterOnEnter = null,

					Action<StateBase<TStateId>> beforeOnLogic = null,
					Action<StateBase<TStateId>> afterOnLogic = null,

					Action<StateBase<TStateId>> beforeOnExit = null,
					Action<StateBase<TStateId>> afterOnExit = null) : base(state.needsExitTime)
			{
				this.state = state;

				this.beforeOnEnter = beforeOnEnter;
				this.afterOnEnter = afterOnEnter;

				this.beforeOnLogic = beforeOnLogic;
				this.afterOnLogic = afterOnLogic;

				this.beforeOnExit = beforeOnExit;
				this.afterOnExit = afterOnExit;
			}

			public override void Init()
			{
				state.name = name;
				state.fsm = fsm;

				state.Init();
			}

			public override void OnEnter()
			{
				beforeOnEnter?.Invoke(this);
				state.OnEnter();
				afterOnEnter?.Invoke(this);
			}

			public override void OnLogic()
			{
				beforeOnLogic?.Invoke(this);
				state.OnLogic();
				afterOnLogic?.Invoke(this);
			}

			public override void OnExit()
			{
				beforeOnExit?.Invoke(this);
				state.OnExit();
				afterOnExit?.Invoke(this);
			}

			public override void OnExitRequest()
			{
				state.OnExitRequest();
			}

			public void Trigger(TEvent trigger)
			{
				(state as ITriggerable<TEvent>)?.Trigger(trigger);
			}

			public void OnAction(TEvent trigger) {
				(state as IActionable<TEvent>)?.OnAction(trigger);
			}

			public void OnAction<TData>(TEvent trigger, TData data) {
				(state as IActionable<TEvent>)?.OnAction<TData>(trigger, data);
			}
		}

		private Action<StateBase<TStateId>>
			beforeOnEnter,
			afterOnEnter,

			beforeOnLogic,
			afterOnLogic,

			beforeOnExit,
			afterOnExit;

		/// <summary>
		/// Initialises a new instance of the StateWrapper class
		/// </summary>
		public StateWrapper(
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

		public WrappedState Wrap(StateBase<TStateId> state)
		{
			return new WrappedState(
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

	public class StateWrapper : StateWrapper<string, string>
	{
		public StateWrapper(
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
