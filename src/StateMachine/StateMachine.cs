using System.Collections.Generic;

/**
 * Hierarchichal finite state machine for Unity
 * by Inspiaaa
 *
 * Version: 1.9.0
 */

namespace FSM
{
	/// <summary>
	/// A finite state machine that can also be used as a state of a parent state machine to create
	/// a hierarchy (-> hierarchical state machine)
	/// </summary>
	public class StateMachine<TOwnId, TStateId, TEvent> :
		StateBase<TOwnId>,
		ITriggerable<TEvent>,
		IStateMachine<TStateId>,
		IActionable<TEvent>
	{
		/// <summary>
		/// A bundle of a state together with the outgoing transitions and trigger transitions.
		/// It's useful, as you only need to do one Dictionary lookup for these three items.
		/// => Much better performance
		/// </summary>
		private class StateBundle
		{
			// By default, these fields are all null and only get a value when you need them
			// => Lazy evaluation => Memory efficient, when you only need a subset of features
			public StateBase<TStateId> state;
			public List<TransitionBase<TStateId>> transitions;
			public Dictionary<TEvent, List<TransitionBase<TStateId>>> triggerToTransitions;

			public void AddTransition(TransitionBase<TStateId> t)
			{
				transitions = transitions ?? new List<TransitionBase<TStateId>>();
				transitions.Add(t);
			}

			public void AddTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition) {
				triggerToTransitions = triggerToTransitions
					?? new Dictionary<TEvent, List<TransitionBase<TStateId>>>();

				List<TransitionBase<TStateId>> transitionsOfTrigger;

				if (! triggerToTransitions.TryGetValue(trigger, out transitionsOfTrigger))
				{
					transitionsOfTrigger = new List<TransitionBase<TStateId>>();
					triggerToTransitions.Add(trigger, transitionsOfTrigger);
				}

				transitionsOfTrigger.Add(transition);
			}
		}

		// A cached empty list of transitions (For improved readability, less GC)
		private static readonly List<TransitionBase<TStateId>> noTransitions
			= new List<TransitionBase<TStateId>>(0);
		private static readonly Dictionary<TEvent, List<TransitionBase<TStateId>>> noTriggerTransitions
			= new Dictionary<TEvent, List<TransitionBase<TStateId>>>(0);

		private (TStateId state, bool hasState) startState = (default, false);
		private (TStateId state, bool isPending) pendingState = (default, false);

		// Central storage of states
		private Dictionary<TStateId, StateBundle> nameToStateBundle
			= new Dictionary<TStateId, StateBundle>();

		private StateBase<TStateId> activeState = null;
		private List<TransitionBase<TStateId>> activeTransitions = noTransitions;
		private Dictionary<TEvent, List<TransitionBase<TStateId>>> activeTriggerTransitions = noTriggerTransitions;

		private List<TransitionBase<TStateId>> transitionsFromAny
			= new List<TransitionBase<TStateId>>();
		private Dictionary<TEvent, List<TransitionBase<TStateId>>> triggerTransitionsFromAny
			= new Dictionary<TEvent, List<TransitionBase<TStateId>>>();

		public StateBase<TStateId> ActiveState
		{
			get
			{
				EnsureIsInitializedFor("Trying to get the active state");
				return activeState;
			}
		}
		public TStateId ActiveStateName => ActiveState.name;

		private bool IsActive => activeState != null;
		private bool IsRootFsm => fsm == null;

		/// <summary>
		/// Initialises a new instance of the StateMachine class
		/// </summary>
		/// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determins whether the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until the active state is ready for a
		/// 	state change (true).</param>
		public StateMachine(bool needsExitTime = true, bool isGhostState = false, bool isExitState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState, isExitState: isExitState)
		{

		}

		/// <summary>
		/// Throws an exception if the state machine is not initialised yet.
		/// </summary>
		/// <param name="context">String message for which action the fsm should
		/// 	be initialised for.</param>
		private void EnsureIsInitializedFor(string context)
		{
			if (activeState == null)
				throw new FSM.Exceptions.StateMachineNotInitializedException(context);
		}

		/// <summary>
		/// Notifies the state machine that the state can cleanly exit,
		/// and if a state change is pending, it will execute it.
		/// </summary>
		/// <returns>Returns true if the state machine could execute a pending state change
		/// 	and false if it remained in its current state</returns>
		public bool StateCanExit()
		{
			if (TryToExitStateMachine())
			{
				return true;
			}

			if (!pendingState.isPending)
			{
				return false;
			}

			TStateId state = pendingState.state;
			// When the pending state is a ghost state, ChangeState() will have
			// to try all outgoing transitions, which may overwrite the pendingState.
			// That's why it is first cleared, and not afterwards, as that would overwrite
			// a new, valid pending state.
			pendingState = (default, false);
			ChangeState(state);

			TryToExitStateMachine();
			return true;
		}

		/// <summary>
		/// Notifies the state machine that it can cleanly exit so that the parent state machine
		/// can move on to another state.
		/// </summary>
		/// <returns>Returns true if this state machine has exited, i.e. the parent state machine
		/// 	has moved on to another state. Returns false if this state machine
		/// 	remains active.</returns>
		public bool StateMachineCanExit()
		{
			if (fsm == null) {
				return false;
			}
			return fsm.StateCanExit();
		}

		public override void OnExitRequest()
		{
			if (! activeState.isExitState)
			{
				return;
			}

			if (activeState.needsExitTime)
			{
				activeState.OnExitRequest();
			}
			else
			{
				StateMachineCanExit();
			}
		}

		/// <summary>
		/// Instantly changes to the target state
		/// </summary>
		/// <param name="name">The name / identifier of the active state</param>
		private void ChangeState(TStateId name)
		{
			activeState?.OnExit();

			StateBundle bundle;

			if (!nameToStateBundle.TryGetValue(name, out bundle) || bundle.state == null)
			{
				throw new FSM.Exceptions.StateNotFoundException<TStateId>(name, "Switching states");
			}

			activeTransitions = bundle.transitions ?? noTransitions;
			activeTriggerTransitions = bundle.triggerToTransitions ?? noTriggerTransitions;

			activeState = bundle.state;
			activeState.OnEnter();

			for (int i = 0; i < activeTransitions.Count; i++)
			{
				activeTransitions[i].OnEnter();
			}

			foreach (List<TransitionBase<TStateId>> transitions in activeTriggerTransitions.Values)
			{
				for (int i = 0; i < transitions.Count; i++)
				{
					transitions[i].OnEnter();
				}
			}

			if (activeState.isGhostState)
			{
				TryToExitGhostState();
			}
		}

		/// <summary>
		/// Tries to instantly exit the active ghost state, by attempting to make the
		/// state machine exit (and give control to a parent state machine) and by
		/// checking all direct outgoing transitions.
		/// </summary>
		private void TryToExitGhostState()
		{
			if (TryToExitStateMachine()) return;
			TryAllDirectTransitions();
		}

		/// <summary>
		/// If the active state is an exit state, it tries to exit the current state
		/// machine so that a parent state machine can move on to another state.
		/// </summary>
		private bool TryToExitStateMachine()
		{
			if (fsm == null || !activeState.isExitState)
			{
				return false;
			}

			if (activeState.needsExitTime) {
				activeState.OnExitRequest();
				return !IsActive;
			}

			return fsm.StateCanExit();
		}

		/// <summary>
		/// Requests a state change, respecting the <c>needsExitTime</c> property of the active state
		/// </summary>
		/// <param name="name">The name / identifier of the target state</param>
		/// <param name="forceInstantly">Overrides the needsExitTime of the active state if true,
		/// therefore forcing an immediate state change</param>
		public void RequestStateChange(TStateId name, bool forceInstantly = false)
		{
			if (!activeState.needsExitTime || forceInstantly)
			{
				ChangeState(name);
				TryToExitStateMachine();
			}
			else
			{
				pendingState = (name, true);
				activeState.OnExitRequest();
				/**
				 * If it can exit, the activeState would call
				 * -> state.fsm.StateCanExit() which in turn would call
				 * -> fsm.ChangeState(...)
				 */
			}
		}

		/// <summary>
		/// Checks if a transition can take place, and if this is the case, transition to the
		/// "to" state and return true. Otherwise it returns false.
		/// </summary>
		/// <param name="transition"></param>
		/// <returns></returns>
		private bool TryTransition(TransitionBase<TStateId> transition)
		{
			if (!transition.ShouldTransition())
				return false;

			RequestStateChange(transition.to, transition.forceInstantly);

			return true;
		}

		/// <summary>
		/// Defines the entry point of the state machine
		/// </summary>
		/// <param name="name">The name / identifier of the start state</param>
		public void SetStartState(TStateId name)
		{
			startState = (name, true);
		}

		/// <summary>
		/// Calls OnEnter if it is the root machine, therefore initialising the state machine
		/// </summary>
		public override void Init()
		{
			if (!IsRootFsm) return;

			OnEnter();
		}

		/// <summary>
		/// Initialises the state machine and must be called before OnLogic is called.
		/// It sets the activeState to the selected startState.
		/// </summary>
		public override void OnEnter()
		{
			if (!startState.hasState)
			{
				throw new System.InvalidOperationException(
					FSM.Exceptions.ExceptionFormatter.Format(
						context: "Running OnEnter of the state machine.",
						problem: "No start state is selected. "
							+ "The state machine needs at least one state to function properly.",
						solution: "Make sure that there is at least one state in the state machine "
							+ "before running Init() or OnEnter() by calling fsm.AddState(...)."
					)
				);
			}

			ChangeState(startState.state);

			for (int i = 0; i < transitionsFromAny.Count; i ++)
			{
				transitionsFromAny[i].OnEnter();
			}

			foreach (List<TransitionBase<TStateId>> transitions in triggerTransitionsFromAny.Values)
			{
				for (int i = 0; i < transitions.Count; i ++)
				{
					transitions[i].OnEnter();
				}
			}
		}

		/// <summary>
		/// Tries the "global" transitions that can transition from any state
		/// </summary>
		/// <returns>Returns true if a transition occurred.</returns>
		private bool TryAllGlobalTransitions()
		{
			for (int i = 0; i < transitionsFromAny.Count; i++)
			{
				TransitionBase<TStateId> transition = transitionsFromAny[i];

				// Don't transition to the "to" state, if that state is already the active state
				if (EqualityComparer<TStateId>.Default.Equals(transition.to, activeState.name))
					continue;

				if (TryTransition(transition))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Tries the "normal" transitions that transition from one specific state to another.
		/// </summary>
		/// <returns>Returns true if a transition occurred.</returns>
		private bool TryAllDirectTransitions()
		{
			for (int i = 0; i < activeTransitions.Count; i++)
			{
				TransitionBase<TStateId> transition = activeTransitions[i];

				if (TryTransition(transition))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Runs one logic step. It does at most one transition itself and
		/// calls the active state's logic function (after the state transition, if
		/// one occurred).
		/// </summary>
		public override void OnLogic()
		{
			EnsureIsInitializedFor("Running OnLogic");

			bool hasChangedState = TryAllGlobalTransitions();

			if (!hasChangedState) {
				TryAllDirectTransitions();
			}

			// When a direct transitions leads to an exit state and the state machine exits,
			// it exits before OnLogic() has finished calling and therefore activeState will be null.
			activeState?.OnLogic();
		}

		public override void OnExit()
		{
			if (activeState != null)
			{
				activeState.OnExit();
				// By setting the activeState to null, the state's onExit method won't be called
				// a second time when the state machine enters again (and changes to the start state)
				activeState = null;
			}
		}

		/// <summary>
		/// Gets the StateBundle belonging to the <c>name</c> state "slot" if it exists.
		/// Otherwise it will create a new StateBundle, that will be added to the Dictionary,
		/// and return the newly created instance.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private StateBundle GetOrCreateStateBundle(TStateId name) {
			StateBundle bundle;

			if (! nameToStateBundle.TryGetValue(name, out bundle)) {
				bundle = new StateBundle();
				nameToStateBundle.Add(name, bundle);
			}

			return bundle;
		}

		/// <summary>
		/// Adds a new node / state to the state machine.
		/// </summary>
		/// <param name="name">The name / identifier of the new state</param>
		/// <param name="state">The new state instance, e.g. <c>State</c>, <c>CoState</c>, <c>StateMachine</c></param>
		public void AddState(TStateId name, StateBase<TStateId> state)
		{
			state.fsm = this;
			state.name = name;
			state.Init();

			StateBundle bundle = GetOrCreateStateBundle(name);
			bundle.state = state;

			if (nameToStateBundle.Count == 1 && !startState.hasState)
			{
				SetStartState(name);
			}
		}

		/// <summary>
		/// Initialises a transition, i.e. sets its fsm attribute, and then calls its Init method.
		/// </summary>
		/// <param name="transition"></param>
		private void InitTransition(TransitionBase<TStateId> transition)
		{
			transition.fsm = this;
			transition.Init();
		}

		/// <summary>
		/// Adds a new transition between two states.
		/// </summary>
		/// <param name="transition">The transition instance</param>
		public void AddTransition(TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			StateBundle bundle = GetOrCreateStateBundle(transition.from);
			bundle.AddTransition(transition);
		}

		/// <summary>
		/// Adds a new transition that can happen from any possible state
		/// </summary>
		/// <param name="transition">The transition instance; The "from" field can be
		/// left empty, as it has no meaning in this context.</param>
		public void AddTransitionFromAny(TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			transitionsFromAny.Add(transition);
		}

		/// <summary>
		/// Adds a new trigger transition between two states that is only checked
		/// when the specified trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		/// <param name="transition">The transition instance, e.g. Transition, TransitionAfter, ...</param>
		public void AddTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			StateBundle bundle = GetOrCreateStateBundle(transition.from);
			bundle.AddTriggerTransition(trigger, transition);
		}

		/// <summary>
		/// Adds a new trigger transition that can happen from any possible state, but is only
		/// checked when the specified trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		/// <param name="transition">The transition instance; The "from" field can be
		/// left empty, as it has no meaning in this context.</param>
		public void AddTriggerTransitionFromAny(TEvent trigger, TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			List<TransitionBase<TStateId>> transitionsOfTrigger;

			if (!triggerTransitionsFromAny.TryGetValue(trigger, out transitionsOfTrigger)) {
				transitionsOfTrigger = new List<TransitionBase<TStateId>>();
				triggerTransitionsFromAny.Add(trigger, transitionsOfTrigger);
			}

			transitionsOfTrigger.Add(transition);
		}

		/// <summary>
		/// Adds two transitions:
		/// If the condition of the transition instance is true, it transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// Internally the same transition instance will be used for both transitions
		/// by wrapping it in a ReverseTransition.
		/// </remarks>
		public void AddTwoWayTransition(TransitionBase<TStateId> transition)
		{
			InitTransition(transition);
			AddTransition(transition);

			ReverseTransition<TStateId> reverse = new ReverseTransition<TStateId>(transition, false);
			InitTransition(reverse);
			AddTransition(reverse);
		}

		/// <summary>
		/// Adds two transitions that are only checked when the specified trigger is activated:
		/// If the condition of the transition instance is true, it transitions from the "from"
		/// state to the "to" state. Otherwise it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// Internally the same transition instance will be used for both transitions
		/// by wrapping it in a ReverseTransition.
		/// </remarks>
		public void AddTwoWayTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition)
		{
			InitTransition(transition);
			AddTriggerTransition(trigger, transition);

			ReverseTransition<TStateId> reverse = new ReverseTransition<TStateId>(transition, false);
			InitTransition(reverse);
			AddTriggerTransition(trigger, reverse);
		}

		/// <summary>
		/// Activates the specified trigger, checking all targeted trigger transitions to see whether
		/// a transition should occur.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		/// <returns>True when a transition occurred, otherwise false</returns>
		private bool TryTrigger(TEvent trigger)
		{
			EnsureIsInitializedFor("Checking all trigger transitions of the active state");

			List<TransitionBase<TStateId>> triggerTransitions;

			if (triggerTransitionsFromAny.TryGetValue(trigger, out triggerTransitions))
			{
				for (int i = 0; i < triggerTransitions.Count; i ++)
				{
					TransitionBase<TStateId> transition = triggerTransitions[i];

					if (EqualityComparer<TStateId>.Default.Equals(transition.to, activeState.name))
						continue;

					if (TryTransition(transition))
						return true;
				}
			}

			if (activeTriggerTransitions.TryGetValue(trigger, out triggerTransitions))
			{
				for (int i = 0; i < triggerTransitions.Count; i ++)
				{
					TransitionBase<TStateId> transition = triggerTransitions[i];

					if (TryTransition(transition))
						return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Activates the specified trigger in all active states of the hierarchy, checking all targeted
		/// trigger transitions to see whether a transition should occur.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		public void Trigger(TEvent trigger)
		{
			// If a transition occurs, then the trigger should not be activated
			// in the new active state, that the state machine just switched to.
			if (TryTrigger(trigger)) return;

			(activeState as ITriggerable<TEvent>)?.Trigger(trigger);
		}

		/// <summary>
		/// Only activates the specified trigger locally in this state machine.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		public void TriggerLocally(TEvent trigger)
		{
			TryTrigger(trigger);
		}

		public StateBase<TStateId> GetState(TStateId name)
		{
			StateBundle bundle;

			if (!nameToStateBundle.TryGetValue(name, out bundle) || bundle.state == null)
			{
				throw new FSM.Exceptions.StateNotFoundException<TStateId>(name, "Getting a state");
			}

			return bundle.state;
		}

		/// <summary>
		/// Runs an action on the currently active state.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		public void OnAction(TEvent trigger)
		{
			EnsureIsInitializedFor("Running OnAction of the active state");
			(activeState as IActionable<TEvent>)?.OnAction(trigger);
		}

		/// <summary>
		/// Runs an action on the currently active state and lets you pass one data parameter.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="data">Any custom data for the parameter</param>
		/// <typeparam name="TData">Type of the data parameter.
		/// 	Should match the data type of the action that was added via AddAction<T>(...).</typeparam>
		public void OnAction<TData>(TEvent trigger, TData data)
		{
			EnsureIsInitializedFor("Running OnAction of the active state");
			(activeState as IActionable<TEvent>)?.OnAction<TData>(trigger, data);
		}

		public StateMachine<string, string, string> this[TStateId name]
		{
			get
			{
				StateBase<TStateId> state = GetState(name);
				StateMachine<string, string, string> subFsm = state as StateMachine<string, string, string>;

				if (subFsm == null)
				{
					throw new System.InvalidOperationException(
						FSM.Exceptions.ExceptionFormatter.Format(
							context: "Getting a nested state machine with the indexer",
							problem: "The selected state is not a state machine.",
							solution: "This method is only there for quickly accessing a nested state machine. "
								+ $"To get the selected state, use GetState(\"{name}\")."
						)
					);
				}

				return subFsm;
			}
		}
	}

	// Overloaded classes to allow for an easier usage of the StateMachine for common cases.
	// E.g. new StateMachine() instead of new StateMachine<string, string, string>()

	public class StateMachine<TStateId, TEvent> : StateMachine<TStateId, TStateId, TEvent>
	{
		public StateMachine(bool needsExitTime = true, bool isGhostState = false, bool isExitState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState, isExitState: isExitState)
		{
		}
	}

	public class StateMachine<TStateId> : StateMachine<TStateId, TStateId, string>
	{
		public StateMachine(bool needsExitTime = true, bool isGhostState = false, bool isExitState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState, isExitState: isExitState)
		{
		}
	}

	public class StateMachine : StateMachine<string, string, string>
	{
		public StateMachine(bool needsExitTime = true, bool isGhostState = false, bool isExitState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState, isExitState: isExitState)
		{
		}
	}
}
