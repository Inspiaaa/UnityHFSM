using System;

namespace FSM
{
	/// <summary>
	/// A class that allows you to run additional functions (companion code)
	/// before and after the wrapped state's code.
	/// It does not interfere with the wrapped state's timing / needsExitTime / ... behaviour.
	/// </summary>
	public class StateWrapper<TEvent>
	{
		public class WrappedState : StateBase<TEvent>, ITriggerable<TEvent>
		{
			private Action<StateBase<TEvent>>
				beforeOnEnter,
				afterOnEnter,

				beforeOnLogic,
				afterOnLogic,

				beforeOnExit,
				afterOnExit;

			private StateBase<TEvent> state;

			public WrappedState(
					StateBase<TEvent> state,

					Action<StateBase<TEvent>> beforeOnEnter = null,
					Action<StateBase<TEvent>> afterOnEnter = null,

					Action<StateBase<TEvent>> beforeOnLogic = null,
					Action<StateBase<TEvent>> afterOnLogic = null,

					Action<StateBase<TEvent>> beforeOnExit = null,
					Action<StateBase<TEvent>> afterOnExit = null) : base(state.needsExitTime)
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
				state.mono = mono;

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

			public override void RequestExit()
			{
				state.RequestExit();
			}

			public void Trigger(TEvent trigger)
			{
				(state as ITriggerable<TEvent>)?.Trigger(trigger);
			}
		}

		private StateBase<TEvent> state;

		private Action<StateBase<TEvent>>
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
				Action<StateBase<TEvent>> beforeOnEnter = null,
				Action<StateBase<TEvent>> afterOnEnter = null,

				Action<StateBase<TEvent>> beforeOnLogic = null,
				Action<StateBase<TEvent>> afterOnLogic = null,

				Action<StateBase<TEvent>> beforeOnExit = null,
				Action<StateBase<TEvent>> afterOnExit = null)
		{
			this.beforeOnEnter = beforeOnEnter;
			this.afterOnEnter = afterOnEnter;

			this.beforeOnLogic = beforeOnLogic;
			this.afterOnLogic = afterOnLogic;

			this.beforeOnExit = beforeOnExit;
			this.afterOnExit = afterOnExit;
		}

		public WrappedState Wrap(StateBase<TEvent> state)
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

	public class StateWrapper : StateWrapper<string>
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
