using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
	/// <summary>
	/// A class that allows you to run additional functions (companion code)
	/// before and after the wrapped state's code.
	/// It does not interfere with the wrapped state's timing / needsExitTime / ... behaviour.
	/// </summary>
	public class StateWrapper : StateBase {
		// TODO: Maybe make the wrapper reusable:
		// var wrapper = new StateWrapper( afterOnLogic: s => MoveCamera() );
		// fsm.AddState("Move", wrapper.Wrap( new State() ) )
		// where wrapper.wrap would return a new instance.
		// The StateWrapper would simply be the definition (and not even extend StateBase)
		// while the .Wrap method would return the WrappedState 

		private StateBase state;

		private Action<StateWrapper> beforeOnEnter;
		private Action<StateWrapper> afterOnEnter;

		private Action<StateWrapper> beforeOnLogic;
		private Action<StateWrapper> afterOnLogic;

		private Action<StateWrapper> beforeOnExit;
		private Action<StateWrapper> afterOnExit;

		/// <summary>
		/// Initialises a new instance of the StateWrapper class
		/// </summary>
		/// <param name="state">The state that should be wrapped</param>
		public StateWrapper (
				StateBase state,

				Action<StateWrapper> beforeOnEnter = null,
				Action<StateWrapper> afterOnEnter = null,

				Action<StateWrapper> beforeOnLogic = null,
				Action<StateWrapper> afterOnLogic = null,

				Action<StateWrapper> beforeOnExit = null,
				Action<StateWrapper> afterOnExit = null) : base(state.needsExitTime) 
		{
			this.state = state;

			this.beforeOnEnter = beforeOnEnter;
			this.afterOnEnter = afterOnEnter;

			this.beforeOnLogic = beforeOnLogic;
			this.afterOnLogic = afterOnLogic;

			this.beforeOnExit = beforeOnExit;
			this.afterOnExit = afterOnExit;
		}

		override public void Init() {
			state.name = name;
			state.fsm = fsm;
			state.mono = mono;

			state.Init();
		}

		override public void OnEnter() {
			beforeOnEnter?.Invoke(this);
			state.OnEnter();
			afterOnEnter?.Invoke(this);
		}

		override public void OnLogic() {
			beforeOnLogic?.Invoke(this);
			state.OnLogic();
			afterOnLogic?.Invoke(this);
		}

		override public void OnExit() {
			beforeOnExit?.Invoke(this);
			state.OnExit();
			afterOnExit?.Invoke(this);
		}

		override public void RequestExit() {
			state.RequestExit();
		}
	}
}
