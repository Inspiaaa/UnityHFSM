using System;

namespace FSM
{
	/// <summary>
	/// The "normal" state class that can run code on Enter, on Logic and on Exit,
	/// while also handling the timing of the next state transition
	/// </summary>
	public class State<TStateId, TEvent> : ActionState<TStateId, TEvent>
	{
		private Action<State<TStateId, TEvent>> onEnter;
		private Action<State<TStateId, TEvent>> onLogic;
		private Action<State<TStateId, TEvent>> onExit;
		private Func<State<TStateId, TEvent>, bool> canExit;

		public ITimer timer;

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
				Action<State<TStateId, TEvent>> onEnter = null,
				Action<State<TStateId, TEvent>> onLogic = null,
				Action<State<TStateId, TEvent>> onExit = null,
				Func<State<TStateId, TEvent>, bool> canExit = null,
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

		public override void OnExitRequest()
		{
			if (!needsExitTime || canExit != null && canExit(this))
			{
				fsm.StateCanExit();
			}
		}
	}

	public class State<TStateId> : State<TStateId, string>
	{
		public State(
			Action<State<TStateId, string>> onEnter = null,
			Action<State<TStateId, string>> onLogic = null,
			Action<State<TStateId, string>> onExit = null,
			Func<State<TStateId, string>, bool> canExit = null,
			bool needsExitTime = false) : base(onEnter, onLogic, onExit, canExit, needsExitTime)
		{
		}
	}

	public class State : State<string, string>
	{
		public State(
			Action<State<string, string>> onEnter = null,
			Action<State<string, string>> onLogic = null,
			Action<State<string, string>> onExit = null,
			Func<State<string, string>, bool> canExit = null,
			bool needsExitTime = false) : base(onEnter, onLogic, onExit, canExit, needsExitTime)
		{
		}
	}
}
