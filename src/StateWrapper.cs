using System;

namespace FSM {
	/// <summary>
	/// A class that allows you to run additional functions (companion code)
	/// before and after the wrapped state's code.
	/// It does not interfere with the wrapped state's timing / needsExitTime / ... behaviour.
	/// </summary>
	public class StateWrapper {

		public class WrappedState : StateBase {
			private Action<StateBase>
				beforeOnEnter,
				afterOnEnter,

				beforeOnLogic,
				afterOnLogic,

				beforeOnExit,
				afterOnExit;
			
			private StateBase state;

			public WrappedState (
					StateBase state,

					Action<StateBase> beforeOnEnter = null,
					Action<StateBase> afterOnEnter = null,

					Action<StateBase> beforeOnLogic = null,
					Action<StateBase> afterOnLogic = null,

					Action<StateBase> beforeOnExit = null,
					Action<StateBase> afterOnExit = null) : base(state.needsExitTime) 
			{
				this.state = state;

				this.beforeOnEnter = beforeOnEnter;
				this.afterOnEnter = afterOnEnter;

				this.beforeOnLogic = beforeOnLogic;
				this.afterOnLogic = afterOnLogic;

				this.beforeOnExit = beforeOnExit;
				this.afterOnExit = afterOnExit;
			}

			public override void Init() {
				state.name = name;
				state.fsm = fsm;
				state.mono = mono;

				state.Init();
			}

			public override void OnEnter() {
				beforeOnEnter?.Invoke(this);
				state.OnEnter();
				afterOnEnter?.Invoke(this);
			}

			public override void OnLogic() {
				beforeOnLogic?.Invoke(this);
				state.OnLogic();
				afterOnLogic?.Invoke(this);
			}

			public override void OnExit() {
				beforeOnExit?.Invoke(this);
				state.OnExit();
				afterOnExit?.Invoke(this);
			}

			public override void RequestExit() {
				state.RequestExit();
			}
		}

		private StateBase state;

		private Action<StateBase> 
			beforeOnEnter,
			afterOnEnter,

			beforeOnLogic,
			afterOnLogic,

			beforeOnExit,
			afterOnExit;

		/// <summary>
		/// Initialises a new instance of the StateWrapper class
		/// </summary>
		public StateWrapper (
				Action<StateBase> beforeOnEnter = null,
				Action<StateBase> afterOnEnter = null,

				Action<StateBase> beforeOnLogic = null,
				Action<StateBase> afterOnLogic = null,

				Action<StateBase> beforeOnExit = null,
				Action<StateBase> afterOnExit = null)
		{
			this.beforeOnEnter = beforeOnEnter;
			this.afterOnEnter = afterOnEnter;

			this.beforeOnLogic = beforeOnLogic;
			this.afterOnLogic = afterOnLogic;

			this.beforeOnExit = beforeOnExit;
			this.afterOnExit = afterOnExit;
		}

		public WrappedState Wrap(StateBase state) {
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
}
