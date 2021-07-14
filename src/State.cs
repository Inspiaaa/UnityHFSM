using System;

namespace FSM
{
	/// <summary>
	/// The "normal" state class that can run code on Enter, on Logic and on Exit,
	/// while also handling the timing of the next state transition
	/// </summary>
	public class State<TStateId> : StateBase<TStateId>
	{
		private Action<State<TStateId>> onEnter;
		private Action<State<TStateId>> onLogic;
		private Action<State<TStateId>> onExit;
		private Func<State<TStateId>, bool> canExit;

		public Timer timer;

		/// <summary>
		/// Initialises a new instance of the State class
		/// </summary>
		/// <param name="onEnter">A function that is called when the state machine enters this state</param>
		/// <param name="onLogic">A function that is called by the logic function of the state machine if this state is active</param>
		/// <param name="onExit">A function that is called when the state machine exits this state</param>
		/// <param name="canExit">(Only if needsExitTime is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, later in OnLogic() it should call fsm.StateCanExit()</param>
		/// <param name="needsExitTime">Determins if the state is allowed to instantly
		/// 	exit on a transition (false), or if the state machine should wait until the state is ready for a
		/// 	state change (true)</param>
		public State(
				Action<State<TStateId>> onEnter = null,
				Action<State<TStateId>> onLogic = null,
				Action<State<TStateId>> onExit = null,
				Func<State<TStateId>, bool> canExit = null,
				bool needsExitTime = false) : base(needsExitTime)
		{
			this.onEnter = onEnter;
			this.onLogic = onLogic;
			this.onExit = onExit;
			this.canExit = canExit;

			this.timer = new Timer();
		}

		public override void OnEnter()
		{
			timer.Reset();

			onEnter?.Invoke(this);
		}

		public override void OnLogic()
		{
			onLogic?.Invoke(this);
		}

		public override void OnExit()
		{
			onExit?.Invoke(this);
		}

		public override void RequestExit()
		{
			if (!needsExitTime || canExit != null && canExit(this))
			{
				fsm.StateCanExit();
			}
		}
	}

	public class State : State<string>
	{
		public State(
			Action<State<string>> onEnter = null,
			Action<State<string>> onLogic = null,
			Action<State<string>> onExit = null,
			Func<State<string>, bool> canExit = null,
			bool needsExitTime = false) : base(onEnter, onLogic, onExit, canExit, needsExitTime)
		{
		}
	}
}
