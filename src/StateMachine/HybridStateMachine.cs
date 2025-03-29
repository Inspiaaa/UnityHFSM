using System;

namespace UnityHFSM
{
	/// <summary>
	/// A StateMachine that is also like a normal <see cref="State"/> in the sense that it allows you to run
	/// custom code on enter, on logic, ... besides its active state's code.
	/// It is especially handy for hierarchical state machines, as it allows you to factor out
	/// common code from the sub states into the HybridStateMachines, essentially removing
	/// duplicate code.
	/// The HybridStateMachine can also be seen as a state wrapper / decorator around
	/// a normal <see cref="StateMachine"/>.
	/// </summary>
	public class HybridStateMachine<TOwnId, TStateId, TEvent> : StateMachine<TOwnId, TStateId, TEvent>
	{
		private Action<HybridStateMachine<TOwnId, TStateId, TEvent>>
			beforeOnEnter, afterOnEnter,
			beforeOnLogic, afterOnLogic,
			beforeOnExit, afterOnExit;

		// Lazily initialised
		private ActionStorage<TEvent> actionStorage;

		public Timer timer;

		/// <summary>Initialises a new instance of the HybridStateMachine class.</summary>
		/// <param name="beforeOnEnter">A function that is called before running the sub-state's OnEnter.</param>
		/// <param name="afterOnEnter">A function that is called after running the sub-state's OnEnter.</param>
		/// <param name="beforeOnLogic">A function that is called before running the sub-state's OnLogic.</param>
		/// <param name="afterOnLogic">A function that is called after running the sub-state's OnLogic.</param>
		/// <param name="beforeOnExit">A function that is called before running the sub-state's OnExit.</param>
		/// <param name="afterOnExit">A function that is called after running the sub-state's OnExit.</param>
		/// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determines whether the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until an explicit exit transition occurs.</param>
		/// <inheritdoc cref="StateBase{T}(bool, bool)"/>
		public HybridStateMachine(
				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> beforeOnEnter = null,
				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> afterOnEnter = null,

				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> beforeOnLogic = null,
				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> afterOnLogic = null,

				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> beforeOnExit= null,
				Action<HybridStateMachine<TOwnId, TStateId, TEvent>> afterOnExit = null,

				bool needsExitTime = false,
				bool isGhostState = false,
				bool rememberLastState = false)
				: base(needsExitTime: needsExitTime, isGhostState: isGhostState, rememberLastState: rememberLastState)
		{
			this.beforeOnEnter = beforeOnEnter;
			this.afterOnEnter = afterOnEnter;

			this.beforeOnLogic = beforeOnLogic;
			this.afterOnLogic = afterOnLogic;

			this.beforeOnExit = beforeOnExit;
			this.afterOnExit = afterOnExit;

			this.timer = new Timer();
		}

		public override void OnEnter()
		{
			beforeOnEnter?.Invoke(this);
			base.OnEnter();

			timer.Reset();
			afterOnEnter?.Invoke(this);
		}

		public override void OnLogic()
		{
			beforeOnLogic?.Invoke(this);
			base.OnLogic();
			afterOnLogic?.Invoke(this);
		}

		public override void OnExit()
		{
			beforeOnExit?.Invoke(this);
			base.OnExit();
			afterOnExit?.Invoke(this);
		}

		public override void OnAction(TEvent trigger)
		{
			actionStorage?.RunAction(trigger);
			base.OnAction(trigger);
		}

		public override void OnAction<TData>(TEvent trigger, TData data)
		{
			actionStorage?.RunAction<TData>(trigger, data);
			base.OnAction<TData>(trigger, data);
		}

		/// <summary>
		/// Adds an action that can be called with <c>OnAction()</c>. Actions are like the builtin events
		/// <c>OnEnter</c> / <c>OnLogic</c> / ... but are defined by the user.
		/// The action is run before the sub-state's action.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="action">Function that should be called when the action is run</param>
		/// <returns>Itself</returns>
		public HybridStateMachine<TOwnId, TStateId, TEvent> AddAction(TEvent trigger, Action action)
		{
			actionStorage = actionStorage ?? new ActionStorage<TEvent>();
			actionStorage.AddAction(trigger, action);

			// Fluent interface
			return this;
		}

		/// <summary>
		/// Adds an action that can be called with <c>OnAction&lt;T&gt;()</c>. This overload allows you to
		/// run a function that takes one data parameter.
		/// The action is run before the sub-state's action.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="action">Function that should be called when the action is run</param>
		/// <typeparam name="TData">Data type of the parameter of the function</typeparam>
		/// <returns>Itself</returns>
		public HybridStateMachine<TOwnId, TStateId, TEvent> AddAction<TData>(TEvent trigger, Action<TData> action)
		{
			actionStorage = actionStorage ?? new ActionStorage<TEvent>();
			actionStorage.AddAction<TData>(trigger, action);

			// Fluent interface
			return this;
		}
	}

	/// <inheritdoc />
	public class HybridStateMachine<TStateId, TEvent> : HybridStateMachine<TStateId, TStateId, TEvent>
	{
		/// <inheritdoc />
		public HybridStateMachine(
			Action<HybridStateMachine<TStateId, TStateId, TEvent>> beforeOnEnter = null,
			Action<HybridStateMachine<TStateId, TStateId, TEvent>> afterOnEnter = null,

			Action<HybridStateMachine<TStateId, TStateId, TEvent>> beforeOnLogic = null,
			Action<HybridStateMachine<TStateId, TStateId, TEvent>> afterOnLogic = null,

			Action<HybridStateMachine<TStateId, TStateId, TEvent>> beforeOnExit = null,
			Action<HybridStateMachine<TStateId, TStateId, TEvent>> afterOnExit = null,

			bool needsExitTime = false,
			bool isGhostState = false,
			bool rememberLastState = false) : base(
				beforeOnEnter, afterOnEnter,
				beforeOnLogic, afterOnLogic,
				beforeOnExit, afterOnExit,
				needsExitTime,
				isGhostState,
				rememberLastState
			)
		{
		}
	}

	/// <inheritdoc />
	public class HybridStateMachine<TStateId> : HybridStateMachine<TStateId, TStateId, string>
	{
		/// <inheritdoc />
		public HybridStateMachine(
			Action<HybridStateMachine<TStateId, TStateId, string>> beforeOnEnter = null,
			Action<HybridStateMachine<TStateId, TStateId, string>> afterOnEnter = null,

			Action<HybridStateMachine<TStateId, TStateId, string>> beforeOnLogic = null,
			Action<HybridStateMachine<TStateId, TStateId, string>> afterOnLogic = null,

			Action<HybridStateMachine<TStateId, TStateId, string>> beforeOnExit= null,
			Action<HybridStateMachine<TStateId, TStateId, string>> afterOnExit = null,

			bool needsExitTime = false,
			bool isGhostState = false,
			bool rememberLastState = false) : base(
				beforeOnEnter, afterOnEnter,
				beforeOnLogic, afterOnLogic,
				beforeOnExit, afterOnExit,
				needsExitTime,
				isGhostState,
				rememberLastState
			)
		{
		}
	}

	/// <inheritdoc />
	public class HybridStateMachine : HybridStateMachine<string, string, string>
	{
		/// <inheritdoc />
		public HybridStateMachine(
			Action<HybridStateMachine<string, string, string>> beforeOnEnter = null,
			Action<HybridStateMachine<string, string, string>> afterOnEnter = null,

			Action<HybridStateMachine<string, string, string>> beforeOnLogic = null,
			Action<HybridStateMachine<string, string, string>> afterOnLogic = null,

			Action<HybridStateMachine<string, string, string>> beforeOnExit= null,
			Action<HybridStateMachine<string, string, string>> afterOnExit = null,

			bool needsExitTime = false,
			bool isGhostState = false,
			bool rememberLastState = false) : base(
				beforeOnEnter, afterOnEnter,
				beforeOnLogic, afterOnLogic,
				beforeOnExit, afterOnExit,
				needsExitTime,
				isGhostState,
				rememberLastState
			)
		{
		}
	}
}
