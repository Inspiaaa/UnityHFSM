using System;

namespace FSM
{
	/// <summary>
	/// A State-like StateMachine that allows you to run additional functions (companion code)
	/// with the sub-states.
	/// It is especially handy for hierarchical state machines, as it allows you to factor out
	/// common code from the sub states into the HybridStateMachines, essentially removing
	/// duplicate code.
	/// The HybridStateMachine can also be seen as a StateWrapper around a normal StateMachine.
	/// </summary>
	public class HybridStateMachine<TOwnId, TStateId, TEvent> : StateMachine<TOwnId, TStateId, TEvent>
	{
		private Action<HybridStateMachine<TOwnId, TStateId, TEvent>> onEnter;
		private Action<HybridStateMachine<TOwnId, TStateId, TEvent>> onLogic;
		private Action<HybridStateMachine<TOwnId, TStateId, TEvent>> onExit;

		public Timer timer;

		/// <summary>
		/// Initialises a new instance of the HybridStateMachine class
		/// </summary>
		/// <param name="onEnter">A function that is called after running the sub-state's OnEnter method
		/// when this state machine is entered</param>
		/// <param name="onLogic">A function that is called after running the sub-state's OnLogic method
		/// if this state machine is the active state</param>
		/// <param name="onExit">A function that is called after running the sub-state's OnExit method
		/// when this state machine is left</param>
		/// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determins whether the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until the active state is ready for a
		/// 	state change (true).</param>
		public HybridStateMachine(
				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> onEnter = null,
				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> onLogic = null,
				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> onExit = null,
				bool needsExitTime = false) : base(needsExitTime)
		{
			this.onEnter = onEnter;
			this.onLogic = onLogic;
			this.onExit = onExit;

			this.timer = new Timer();
		}

		public override void OnEnter()
		{
			base.OnEnter();

			timer.Reset();
			onEnter?.Invoke(this);
		}

		public override void OnLogic()
		{
			base.OnLogic();

			onLogic?.Invoke(this);
		}

		public override void OnExit()
		{
			base.OnExit();

			onExit?.Invoke(this);
		}
	}

	public class HybridStateMachine<TStateId, TEvent> : HybridStateMachine<TStateId, TStateId, TEvent>
	{
		public HybridStateMachine(
			Action<HybridStateMachine<TStateId, TStateId, TEvent>> onEnter = null,
			Action<HybridStateMachine<TStateId, TStateId, TEvent>> onLogic = null,
			Action<HybridStateMachine<TStateId, TStateId, TEvent>> onExit = null,
			bool needsExitTime = false) : base(onEnter, onLogic, onExit, needsExitTime)
		{
		}
	}

	public class HybridStateMachine<TStateId> : HybridStateMachine<TStateId, TStateId, string>
	{
		public HybridStateMachine(
			Action<HybridStateMachine<TStateId, TStateId, string>> onEnter = null,
			Action<HybridStateMachine<TStateId, TStateId, string>> onLogic = null,
			Action<HybridStateMachine<TStateId, TStateId, string>> onExit = null,
			bool needsExitTime = false) : base(onEnter, onLogic, onExit, needsExitTime)
		{
		}
	}

	public class HybridStateMachine : HybridStateMachine<string, string, string>
	{
		public HybridStateMachine(
			Action<HybridStateMachine<string, string, string>> onEnter = null,
			Action<HybridStateMachine<string, string, string>> onLogic = null,
			Action<HybridStateMachine<string, string, string>> onExit = null,
			bool needsExitTime = false) : base(onEnter, onLogic, onExit, needsExitTime)
		{
		}
	}
}
