using System;
using System.Collections.Generic;

namespace UnityHFSM
{
	/// <summary>
	/// A state that can run multiple states in parallel.
	/// </summary>
	public class ParallelStates<TOwnId, TStateId, TEvent> : StateBase<TOwnId>, IActionable<TEvent>, IStateMachine
	{
		private List<StateBase<TStateId>> states = new List<StateBase<TStateId>>();

		// When the states are passed in via the constructor, they are not assigned names / identifiers.
		// This means that the active hierarchy path cannot include them (which would be used for debugging purposes).
		private bool areStatesNameless = false;
		private bool isActive;

		private Func<ParallelStates<TOwnId, TStateId, TEvent>, bool> canExit;

		public bool HasPendingTransition => fsm.HasPendingTransition;
		public IStateMachine ParentFsm => fsm;

		public ParallelStates(
			Func<ParallelStates<TOwnId, TStateId, TEvent>, bool> canExit = null,
			bool needsExitTime = false,
			bool isGhostState = false) : base(needsExitTime, isGhostState)
		{
			this.canExit = canExit;
		}

		public ParallelStates(params StateBase<TStateId>[] states)
			: this(null, false, false, states) { }

		public ParallelStates(bool needsExitTime, params StateBase<TStateId>[] states)
			: this(null, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<TOwnId, TStateId, TEvent>, bool> canExit,
			bool needsExitTime,
			params StateBase<TStateId>[] states) : this(canExit, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<TOwnId, TStateId, TEvent>, bool> canExit,
			bool needsExitTime,
			bool isGhostState,
			params StateBase<TStateId>[] states) : base(needsExitTime, isGhostState)
		{
			this.canExit = canExit;
			this.areStatesNameless = true;

			foreach (var state in states) {
				AddState(default, state);
			}
		}

		public ParallelStates<TOwnId, TStateId, TEvent> AddState(TStateId id, StateBase<TStateId> state)
		{
			state.fsm = this;
			state.name = id;
			state.Init();

			states.Add(state);

			// Fluent interface.
			return this;
		}

		public override void Init()
		{
			foreach (var state in states)
			{
				state.fsm = this;
			}
		}

		public override void OnEnter()
		{
			isActive = true;

			foreach (var state in states)
			{
				state.OnEnter();
			}
		}

		public override void OnLogic()
		{
			foreach (var state in states)
			{
				state.OnLogic();
			}

			if (needsExitTime && canExit != null && fsm.HasPendingTransition && canExit(this))
			{
				fsm.StateCanExit();
			}
		}

		public override void OnExit()
		{
			isActive = false;

			foreach (var state in states)
			{
				state.OnExit();
			}
		}

		public override void OnExitRequest()
		{
			// When this state machine is requested to exit, check each child state to see if any one is
			// ready to exit. This behaviour can be overridden by providing a canExit function.
			if (canExit == null)
			{
				foreach (var state in states)
				{
					state.OnExitRequest();
				}
			}
			else {
				if (fsm.HasPendingTransition && canExit(this))
				{
					fsm.StateCanExit();
				}
			}
		}

		public void OnAction(TEvent trigger)
		{
			foreach (var state in states)
			{
				(state as IActionable<TEvent>)?.OnAction(trigger);
			}
		}

		public void OnAction<TData>(TEvent trigger, TData data)
		{
			foreach (var state in states)
			{
				(state as IActionable<TEvent>)?.OnAction(trigger, data);
			}
		}

		public void StateCanExit() {
			// Try to exit as soon as any one of the child states can exit, unless the exit behaviour
			// is overridden by canExit.
			if (isActive && canExit == null) {
				fsm.StateCanExit();
			}
		}

		public override string GetActiveHierarchyPath()
		{
			if (areStatesNameless) {
				// Example path: "Parallel"
				return name.ToString();
			}

			if (states.Count == 1) {
				// Example path: "Parallel/Move"
				return name.ToString() + "/" + states[0].GetActiveHierarchyPath();
			}

			// Example path: "Parallel/(Move & Attack/Shoot)"
			string path = name.ToString() + "/(";

			for (int i = 0; i < states.Count; i ++) {
				path += states[i].GetActiveHierarchyPath();
				if (i < states.Count - 1) {
					path += " & ";
				}
			}

			return path + ")";
		}
	}

	public class ParallelStates<TStateId, TEvent> : ParallelStates<TStateId, TStateId, TEvent>
	{
		public ParallelStates(
			Func<ParallelStates<TStateId, TStateId, TEvent>, bool> canExit = null,
			bool needsExitTime = false,
			bool isGhostState = false) : base(canExit, needsExitTime, isGhostState) { }

		public ParallelStates(params StateBase<TStateId>[] states)
			: base(null, false, false, states) { }

		public ParallelStates(bool needsExitTime, params StateBase<TStateId>[] states)
			: base(null, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<TStateId, TStateId, TEvent>, bool> canExit,
			bool needsExitTime,
			params StateBase<TStateId>[] states) : base(canExit, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<TStateId, TStateId, TEvent>, bool> canExit,
			bool needsExitTime,
			bool isGhostState,
			params StateBase<TStateId>[] states) : base(canExit, needsExitTime, isGhostState, states) { }
	}

	public class ParallelStates<TStateId> : ParallelStates<TStateId, TStateId, string>
	{
		public ParallelStates(
			Func<ParallelStates<TStateId, TStateId, string>, bool> canExit = null,
			bool needsExitTime = false,
			bool isGhostState = false) : base(canExit, needsExitTime, isGhostState) { }

		public ParallelStates(params StateBase<TStateId>[] states)
			: base(null, false, false, states) { }

		public ParallelStates(bool needsExitTime, params StateBase<TStateId>[] states)
			: base(null, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<TStateId, TStateId, string>, bool> canExit,
			bool needsExitTime,
			params StateBase<TStateId>[] states) : base(canExit, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<TStateId, TStateId, string>, bool> canExit,
			bool needsExitTime,
			bool isGhostState,
			params StateBase<TStateId>[] states) : base(canExit, needsExitTime, isGhostState, states) { }
	}

	public class ParallelStates : ParallelStates<string, string, string>
	{
		public ParallelStates(
			Func<ParallelStates<string, string, string>, bool> canExit = null,
			bool needsExitTime = false,
			bool isGhostState = false) : base(canExit, needsExitTime, isGhostState) { }


		public ParallelStates(params StateBase<string>[] states)
			: this(null, false, false, states) { }

		public ParallelStates(bool needsExitTime, params StateBase<string>[] states)
			: this(null, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<string, string, string>, bool> canExit,
			bool needsExitTime,
			params StateBase<string>[] states) : this(canExit, needsExitTime, false, states) { }

		public ParallelStates(
			Func<ParallelStates<string, string, string>, bool> canExit,
			bool needsExitTime,
			bool isGhostState,
			params StateBase<string>[] states) : base(canExit, needsExitTime, isGhostState)
		{
			for (int i = 0; i < states.Length; i ++)
			{
				AddState(i.ToString(), states[i]);
			}
		}
	}
}