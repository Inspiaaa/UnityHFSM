using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityHFSM.Inspection;

/**
 * Hierarchical Finite State Machine for Unity
 * by Inspiaaa and contributors
 *
 * Version: 2.2.0
 */

namespace UnityHFSM
{
	/// <summary>
	/// Main finite state machine class. It can be used as a child state of another state machine
	/// in order to create a hierarchical state machine.
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
			// By default, these fields are all null and only get a value when you need them.
			// => Lazy evaluation => Memory efficient, when you only need a subset of features
			public StateBase<TStateId> state;
			public List<TransitionBase<TStateId>> transitions;
			public Dictionary<TEvent, List<TransitionBase<TStateId>>> triggerToTransitions;

			public void AddTransition(TransitionBase<TStateId> t)
			{
				transitions = transitions ?? new List<TransitionBase<TStateId>>();
				transitions.Add(t);
			}

			public void AddTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition)
			{
				triggerToTransitions = triggerToTransitions
					?? new Dictionary<TEvent, List<TransitionBase<TStateId>>>();

				List<TransitionBase<TStateId>> transitionsOfTrigger;

				if (!triggerToTransitions.TryGetValue(trigger, out transitionsOfTrigger))
				{
					transitionsOfTrigger = new List<TransitionBase<TStateId>>();
					triggerToTransitions.Add(trigger, transitionsOfTrigger);
				}

				transitionsOfTrigger.Add(transition);
			}
		}

		/// <summary>
		/// Represents a delayed / pending transition.
		/// </summary>
		/// <remarks>
		/// This struct is mutable and its methods mutate the state of the struct. This requires great
		/// caution ("mutable structs are evil"), but has lead to a significant increase in performance.
		/// </remarks>
		private struct PendingTransition
		{
			// The following fields have been arranged so that they minimise the size of this struct type,
			// specifically for small TStateId types (see automatic sequential layout of structs).

			// Optional (may be null), used for callbacks when the transition succeeds.
			public ITransitionListener listener;

			public TStateId targetState;

			// As this type is not nullable (it is a value type), an additional field is required
			// to see if the pending transition has been set yet.
			public bool isPending;

			public bool isExitTransition;

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Clear()
			{
				// It suffices just to clear this field, as the other fields are not checked when
				// isPending is false.
				this.isPending = false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void SetToExit(ITransitionListener listener = null)
			{
				this.listener = listener;
				this.isExitTransition = true;
				this.isPending = true;
				// The targetState is irrelevant in this case.
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void SetToState(TStateId target, ITransitionListener listener = null)
			{
				this.listener = listener;
				this.targetState = target;
				this.isExitTransition = false;
				this.isPending = true;
			}
		}

		// A cached empty list of transitions (For improved readability, less GC).
		private static readonly List<TransitionBase<TStateId>> noTransitions
			= new List<TransitionBase<TStateId>>(0);
		private static readonly Dictionary<TEvent, List<TransitionBase<TStateId>>> noTriggerTransitions
			= new Dictionary<TEvent, List<TransitionBase<TStateId>>>(0);

		/// <summary>
		/// Event that is raised when the active state changes.
		/// </summary>
		/// <remarks>
		/// It is triggered when the state machine enters its initial state, and after a transition is performed.
		/// Note that it is not called when the state machine exits.
		/// </remarks>
		public event Action<StateBase<TStateId>> StateChanged;

		private (TStateId state, bool hasState) startState = (default, false);
		private PendingTransition pendingTransition = default;
		private bool rememberLastState = false;

		// Central storage of states.
		private readonly Dictionary<TStateId, StateBundle> stateBundlesByName
			= new Dictionary<TStateId, StateBundle>();

		private StateBase<TStateId> activeState = null;
		private List<TransitionBase<TStateId>> activeTransitions = noTransitions;
		private Dictionary<TEvent, List<TransitionBase<TStateId>>> activeTriggerTransitions = noTriggerTransitions;

		private readonly List<TransitionBase<TStateId>> transitionsFromAny
			= new List<TransitionBase<TStateId>>();
		private readonly Dictionary<TEvent, List<TransitionBase<TStateId>>> triggerTransitionsFromAny
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

		public TStateId PendingStateName => pendingTransition.targetState;
		public StateBase<TStateId> PendingState => GetState(PendingStateName);
		public bool HasPendingTransition => pendingTransition.isPending;

		public IStateTimingManager ParentFsm => fsm;

		public bool IsRootFsm => fsm == null;

		/// <summary>
		/// Initialises a new instance of the StateMachine class.
		/// </summary>
		/// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determines whether the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until an explicit exit transition occurs.</param>
		/// <param name="rememberLastState">(Only for hierarchical states):
		/// 	If true, the state machine will return to its last active state when it enters, instead
		/// 	of to its original start state.</param>
		/// <inheritdoc cref="StateBase{T}(bool, bool)"/>
		public StateMachine(bool needsExitTime = false, bool isGhostState = false, bool rememberLastState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState)
		{
			this.rememberLastState = rememberLastState;
		}

		/// <summary>
		/// Throws an exception if the state machine is not initialised yet.
		/// </summary>
		/// <param name="context">String message for which action the fsm should be initialised for.</param>
		private void EnsureIsInitializedFor(string context)
		{
			if (activeState == null)
				throw UnityHFSM.Exceptions.Common.NotInitialized(this, context);
		}

		/// <summary>
		/// Notifies the state machine that the active state can cleanly exit. If a transition is pending,
		/// the state machine will execute it now.
		/// </summary>
		/// <remarks>
		/// This signal is only valid for this exact point in time. It does not tell the state machine that
		/// it is still safe to perform a transition at a later point in the future; it is not "saved" or
		/// remembered. <para />
		/// As it only has an effect when a transition is pending and transitions are only ever
		/// checked after the <c>OnEnter</c> call, calling this method during <c>OnEnter</c>
		/// has no effect.
		/// </remarks>
		public void StateCanExit()
		{
			if (!pendingTransition.isPending)
				return;

			ITransitionListener listener = pendingTransition.listener;
			if (pendingTransition.isExitTransition)
			{
				pendingTransition = default;

				listener?.BeforeTransition();
				PerformVerticalTransition();
				listener?.AfterTransition();
			}
			else
			{
				TStateId state = pendingTransition.targetState;

				// When the pending state is a ghost state, ChangeState() will have
				// to try all outgoing transitions, which may overwrite the pendingState.
				// That's why it is first cleared, and not afterwards, as that would overwrite
				// a new, valid pending state.
				pendingTransition = default;
				ChangeState(state, listener);
			}
		}

		/// <summary>
		/// Instantly changes to the target state.
		/// </summary>
		/// <param name="name">The name / identifier of the active state.</param>
		/// <param name="listener">Optional object that receives callbacks before and after changing state.</param>
		private void ChangeState(TStateId name, ITransitionListener listener = null)
		{
			listener?.BeforeTransition();
			activeState?.OnExit();

			StateBundle bundle;

			if (!stateBundlesByName.TryGetValue(name, out bundle) || bundle.state == null)
			{
				throw UnityHFSM.Exceptions.Common.StateNotFound(this, name.ToString(), context: "Switching states");
			}

			activeTransitions = bundle.transitions ?? noTransitions;
			activeTriggerTransitions = bundle.triggerToTransitions ?? noTriggerTransitions;

			activeState = bundle.state;
			activeState.OnEnter();

			for (int i = 0, count = activeTransitions.Count; i < count; i++)
			{
				activeTransitions[i].OnEnter();
			}

			foreach (List<TransitionBase<TStateId>> transitions in activeTriggerTransitions.Values)
			{
				for (int i = 0, count = transitions.Count; i < count; i++)
				{
					transitions[i].OnEnter();
				}
			}

			listener?.AfterTransition();

			StateChanged?.Invoke(activeState);

			if (activeState.isGhostState)
			{
				TryAllDirectTransitions();
			}
		}

		/// <summary>
		/// Signals to the parent fsm that this fsm can exit which allows the parent
		/// fsm to transition to the next state.
		/// </summary>
		private void PerformVerticalTransition()
		{
			fsm?.StateCanExit();
		}

		/// <summary>
		/// Requests a state change, respecting the <c>needsExitTime</c> property of the active state.
		/// </summary>
		/// <param name="name">The name / identifier of the target state.</param>
		/// <param name="forceInstantly">Overrides the <c>needsExitTime</c> of the active state if true,
		/// 	therefore forcing an immediate state change.</param>
		/// <param name="listener">Optional object that receives callbacks before and after the transition.</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void RequestStateChange(
			TStateId name,
			bool forceInstantly = false,
			ITransitionListener listener = null)
		{
			if (!activeState.needsExitTime || forceInstantly)
			{
				pendingTransition = default;
				ChangeState(name, listener);
			}
			else
			{
				pendingTransition.SetToState(name, listener);
				activeState.OnExitRequest();
				// If it can exit, the activeState would call
				// -> state.fsm.StateCanExit() which in turn would call
				// -> fsm.ChangeState(...)
			}
		}

		/// <summary>
		/// Requests a "vertical transition", allowing the state machine to exit
		/// to allow the parent fsm to transition to the next state. It respects the
		/// needsExitTime property of the active state.
		/// </summary>
		/// <param name="forceInstantly">Overrides the <c>needsExitTime</c> of the active state if true,
		/// 	therefore forcing an immediate state change.</param>
		/// <param name="listener">Optional object that receives callbacks before and after the transition.</param>
		public void RequestExit(bool forceInstantly = false, ITransitionListener listener = null)
		{
			if (!activeState.needsExitTime || forceInstantly)
			{
				pendingTransition.Clear();
				listener?.BeforeTransition();
				PerformVerticalTransition();
				listener?.AfterTransition();
			}
			else
			{
				pendingTransition.SetToExit(listener);
				activeState.OnExitRequest();
			}
		}

		/// <summary>
		/// Checks if a transition can take place, and if this is the case, transition to the
		/// "to" state and return true. Otherwise, it returns false.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private bool TryTransition(TransitionBase<TStateId> transition)
		{
			if (transition.isExitTransition)
			{
				if (fsm == null || !fsm.HasPendingTransition || !transition.ShouldTransition())
					return false;

				RequestExit(transition.forceInstantly, transition as ITransitionListener);
				return true;
			}
			else
			{
				if (!transition.ShouldTransition())
					return false;

				RequestStateChange(transition.to, transition.forceInstantly, transition as ITransitionListener);
				return true;
			}
		}

		/// <summary>
		/// Tries the "global" transitions that can transition from any state.
		/// </summary>
		/// <returns>Returns true if a transition occurred.</returns>
		private bool TryAllGlobalTransitions()
		{
			for (int i = 0, count = transitionsFromAny.Count; i < count; i++)
			{
				TransitionBase<TStateId> transition = transitionsFromAny[i];

				// Don't transition to the "to" state, if that state is already the active state.
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
			for (int i = 0, count = activeTransitions.Count; i < count; i++)
			{
				TransitionBase<TStateId> transition = activeTransitions[i];

				if (TryTransition(transition))
					return true;
			}

			return false;
		}

		/// <summary>
		/// Calls <c>OnEnter</c> if it is the root state machine, therefore initialising the state machine.
		/// </summary>
		public override void Init()
		{
			if (!IsRootFsm) return;

			OnEnter();
		}

		/// <summary>
		/// Initialises the state machine and must be called before <c>OnLogic</c> is called.
		/// It sets the activeState to the selected startState.
		/// </summary>
		public override void OnEnter()
		{
			if (!startState.hasState)
			{
				throw UnityHFSM.Exceptions.Common.MissingStartState(this, context: "Running OnEnter of the state machine.");
			}

			// Clear any previous pending transition from the last run.
			pendingTransition.Clear();

			ChangeState(startState.state);

			for (int i = 0, count = transitionsFromAny.Count; i < count; i++)
			{
				transitionsFromAny[i].OnEnter();
			}

			foreach (List<TransitionBase<TStateId>> transitions in triggerTransitionsFromAny.Values)
			{
				for (int i = 0, count = transitions.Count; i < count; i++)
				{
					transitions[i].OnEnter();
				}
			}
		}

		/// <summary>
		/// Runs one logic step. It performs at most one transition itself and
		/// calls the active state's logic function (after the state transition,
		/// if one occurred).
		/// </summary>
		public override void OnLogic()
		{
			EnsureIsInitializedFor("Running OnLogic");

			if (TryAllGlobalTransitions())
				goto runOnLogic;

			if (TryAllDirectTransitions())
				goto runOnLogic;

			runOnLogic:
			activeState?.OnLogic();
		}

		public override void OnExit()
		{
			if (activeState == null)
				return;

			if (rememberLastState)
			{
				startState = (activeState.name, true);
			}

			activeState.OnExit();
			// By setting the activeState to null, the state's onExit method won't be called
			// a second time when the state machine enters again (and changes to the start state).
			activeState = null;
		}

		public override void OnExitRequest()
		{
			if (activeState.needsExitTime)
				activeState.OnExitRequest();
		}

		/// <summary>
		/// Defines the entry point of the state machine.
		/// </summary>
		/// <param name="name">The name / identifier of the start state.</param>
		public void SetStartState(TStateId name)
		{
			startState = (name, true);
		}

		/// <summary>
		/// Gets the StateBundle belonging to the <c>name</c> state "slot" if it exists.
		/// Otherwise, it will create a new StateBundle, that will be added to the Dictionary,
		/// and return the newly created instance.
		/// </summary>
		private StateBundle GetOrCreateStateBundle(TStateId name)
		{
			StateBundle bundle;

			if (!stateBundlesByName.TryGetValue(name, out bundle))
			{
				bundle = new StateBundle();
				stateBundlesByName.Add(name, bundle);
			}

			return bundle;
		}

		/// <summary>
		/// Adds a new node / state to the state machine.
		/// </summary>
		/// <param name="name">The name / identifier of the new state.</param>
		/// <param name="state">The new state instance,
		///		e.g. <see cref="State"/>, <see cref="CoState"/>, <see cref="StateMachine"/>.</param>
		public void AddState(TStateId name, StateBase<TStateId> state)
		{
			state.fsm = this;
			state.name = name;
			state.Init();

			StateBundle bundle = GetOrCreateStateBundle(name);
			bundle.state = state;

			if (stateBundlesByName.Count == 1 && !startState.hasState)
			{
				SetStartState(name);
			}
		}

		/// <summary>
		/// Initialises a transition, i.e. sets its <c>fsm</c> attribute, and then calls its <c>Init</c> method.
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
		/// <param name="transition">The transition instance.</param>
		public void AddTransition(TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			StateBundle bundle = GetOrCreateStateBundle(transition.from);
			bundle.AddTransition(transition);
		}

		/// <summary>
		/// Adds a new transition that can happen from any possible state.
		/// </summary>
		/// <param name="transition">The transition instance; The "from" field can be
		/// 	left empty, as it has no meaning in this context.</param>
		public void AddTransitionFromAny(TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			transitionsFromAny.Add(transition);
		}

		/// <summary>
		/// Adds a new trigger transition between two states that is only checked
		/// when the specified trigger is activated.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger.</param>
		/// <param name="transition">The transition instance,
		///		e.g. <see cref="Transition"/>, <see cref="TransitionAfter"/>, ...</param>
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
		/// 	left empty, as it has no meaning in this context.</param>
		public void AddTriggerTransitionFromAny(TEvent trigger, TransitionBase<TStateId> transition)
		{
			InitTransition(transition);

			List<TransitionBase<TStateId>> transitionsOfTrigger;

			if (!triggerTransitionsFromAny.TryGetValue(trigger, out transitionsOfTrigger))
			{
				transitionsOfTrigger = new List<TransitionBase<TStateId>>();
				triggerTransitionsFromAny.Add(trigger, transitionsOfTrigger);
			}

			transitionsOfTrigger.Add(transition);
		}

		/// <summary>
		/// Adds two transitions:
		/// If the condition of the transition instance is true, it transitions from the "from"
		/// state to the "to" state. Otherwise, it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// Internally the same transition instance will be used for both transitions
		/// by wrapping it in a <see cref="ReverseTransition"/>.
		/// For the reverse transition the <c>afterTransition</c> callback is called before the transition
		/// and the <c>onTransition</c> callback afterwards. If this is not desired then replicate the behaviour
		/// of the two-way transitions by creating two separate transitions.
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
		/// state to the "to" state. Otherwise, it performs a transition in the opposite direction,
		/// i.e. from "to" to "from".
		/// </summary>
		/// <remarks>
		/// Internally the same transition instance will be used for both transitions
		/// by wrapping it in a <see cref="ReverseTransition"/>.
		/// For the reverse transition the <c>afterTransition</c> callback is called before the transition
		/// and the <c>onTransition</c> callback afterwards. If this is not desired then replicate the behaviour
		/// of the two-way transitions by creating two separate transitions.
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
		/// Adds a new exit transition from a state. It represents an exit point that
		/// allows the fsm to exit and the parent fsm to continue to the next state.
		/// It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <param name="transition">The transition instance. The "to" field can be
		/// 	left empty, as it has no meaning in this context.</param>
		public void AddExitTransition(TransitionBase<TStateId> transition)
		{
			transition.isExitTransition = true;
			AddTransition(transition);
		}

		/// <summary>
		/// Adds a new exit transition that can happen from any possible state.
		/// It represents an exit point that allows the fsm to exit and the parent fsm to continue
		/// to the next state. It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <param name="transition">The transition instance. The "from" and "to" fields can be
		/// 	left empty, as they have no meaning in this context.</param>
		public void AddExitTransitionFromAny(TransitionBase<TStateId> transition)
		{
			transition.isExitTransition = true;
			AddTransitionFromAny(transition);
		}

		/// <summary>
		/// Adds a new exit transition from a state that is only checked when the specified trigger
		/// is activated.
		/// It represents an exit point that allows the fsm to exit and the parent fsm to continue
		/// to the next state. It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <param name="transition">The transition instance. The "to" field can be
		/// 	left empty, as it has no meaning in this context.</param>
		public void AddExitTriggerTransition(TEvent trigger, TransitionBase<TStateId> transition)
		{
			transition.isExitTransition = true;
			AddTriggerTransition(trigger, transition);
		}

		/// <summary>
		/// Adds a new exit transition that can happen from any possible state and is only checked
		/// when the specified trigger is activated.
		/// It represents an exit point that allows the fsm to exit and the parent fsm to continue
		/// to the next state. It is only checked if the parent fsm has a pending transition.
		/// </summary>
		/// <param name="transition">The transition instance. The "from" and "to" fields can be
		/// 	left empty, as they have no meaning in this context.</param>
		public void AddExitTriggerTransitionFromAny(TEvent trigger, TransitionBase<TStateId> transition)
		{
			transition.isExitTransition = true;
			AddTriggerTransitionFromAny(trigger, transition);
		}

		/// <summary>
		/// Activates the specified trigger, checking all targeted trigger transitions to see whether
		/// a transition should occur.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger.</param>
		/// <returns>True when a transition occurred, otherwise false.</returns>
		private bool TryTrigger(TEvent trigger)
		{
			EnsureIsInitializedFor("Checking all trigger transitions of the active state");

			List<TransitionBase<TStateId>> triggerTransitions;

			if (triggerTransitionsFromAny.TryGetValue(trigger, out triggerTransitions))
			{
				for (int i = 0, count = triggerTransitions.Count; i < count; i++)
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
				for (int i = 0, count = triggerTransitions.Count; i < count; i++)
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
		/// <param name="trigger">The name / identifier of the trigger.</param>
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
		/// <param name="trigger">The name / identifier of the trigger.</param>
		public void TriggerLocally(TEvent trigger)
		{
			TryTrigger(trigger);
		}

		/// <summary>
		/// Runs an action on the currently active state.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		public virtual void OnAction(TEvent trigger)
		{
			EnsureIsInitializedFor("Running OnAction of the active state");
			(activeState as IActionable<TEvent>)?.OnAction(trigger);
		}

		/// <summary>
		/// Runs an action on the currently active state and lets you pass one data parameter.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		/// <param name="data">Any custom data for the parameter.</param>
		/// <typeparam name="TData">Type of the data parameter.
		/// 	Should match the data type of the action that was added via <c>AddAction&lt;T&gt;(...).</c></typeparam>
		public virtual void OnAction<TData>(TEvent trigger, TData data)
		{
			EnsureIsInitializedFor("Running OnAction of the active state");
			(activeState as IActionable<TEvent>)?.OnAction<TData>(trigger, data);
		}

		public StateBase<TStateId> GetState(TStateId name)
		{
			StateBundle bundle;

			if (!stateBundlesByName.TryGetValue(name, out bundle) || bundle.state == null)
			{
				throw UnityHFSM.Exceptions.Common.StateNotFound(this, name.ToString(), context: "Getting a state");
			}

			return bundle.state;
		}

		/// <summary>
		/// Only for state machines using string types: Returns a nested state machine with the given name.
		/// This is a convenience function when working with string hierarchical state machines.
		/// </summary>
		public StateMachine<string, string, string> this[TStateId name]
		{
			get
			{
				StateBase<TStateId> state = GetState(name);
				StateMachine<string, string, string> subFsm = state as StateMachine<string, string, string>;

				if (subFsm == null)
				{
					throw UnityHFSM.Exceptions.Common.QuickIndexerMisusedForGettingState(this, name.ToString());
				}

				return subFsm;
			}
		}

		public override string GetActiveHierarchyPath()
		{
			if (activeState == null)
			{
				// When the state machine is not active, then the active hierarchy path
				// is empty.
				return "";
			}

			return $"{name}/{activeState.GetActiveHierarchyPath()}";
		}

		/// <summary>Returns a list of the names of all currently defined states.</summary>
		/// <remarks>Warning: this is an expensive operation.</remarks>
		public IReadOnlyList<TStateId> GetAllStateNames()
		{
			return stateBundlesByName.Values
				.Where(bundle => bundle.state != null)
				.Select(bundle => bundle.state.name)
				.ToArray();
		}

		/// <summary>Returns a list of all currently defined states.</summary>
		/// <remarks>Warning: this is an expensive operation.</remarks>
		public IReadOnlyList<StateBase<TStateId>> GetAllStates()
		{
			return stateBundlesByName.Values
				.Where(bundle => bundle.state != null)
				.Select(bundle => bundle.state)
				.ToArray();
		}

		public TStateId GetStartStateName()
		{
			if (!startState.hasState)
			{
				throw UnityHFSM.Exceptions.Common.MissingStartState(
					this,
					context: "Getting the start state",
					solution: "Make sure that there is at least one state in the state machine before running "
					+ "GetStartStateName() by calling fsm.AddState(...).");
			}

			return startState.state;
		}

		/// <summary>Returns a list of all added state transitions.</summary>
		/// <remarks>Warning: this is an expensive operation.</remarks>
		public IReadOnlyList<TransitionBase<TStateId>> GetAllTransitions()
		{
			return stateBundlesByName.Values
				.Where(bundle => bundle.transitions != null)
				.SelectMany(bundle => bundle.transitions)
				.ToArray();
		}

		/// <summary>Returns a list of all added state "transitions from any".</summary>
		public IReadOnlyList<TransitionBase<TStateId>> GetAllTransitionsFromAny()
		{
			return transitionsFromAny.ToArray();
		}

		/// <summary>Returns all added trigger transitions, grouped by their trigger events.</summary>
		/// <remarks>Warning: this is an expensive operation.</remarks>
		public IReadOnlyDictionary<TEvent, IReadOnlyList<TransitionBase<TStateId>>> GetAllTriggerTransitions()
		{
			var transitionsByEvent = new Dictionary<TEvent, List<TransitionBase<TStateId>>>();

			foreach (var bundle in stateBundlesByName.Values)
			{
				if (bundle.triggerToTransitions == null)
					continue;

				foreach ((TEvent trigger, List<TransitionBase<TStateId>> transitions) in bundle.triggerToTransitions)
				{
					if (!transitionsByEvent.TryGetValue(trigger, out List<TransitionBase<TStateId>> transitionsForEvent))
					{
						transitionsForEvent = new List<TransitionBase<TStateId>>();
						transitionsByEvent.Add(trigger, transitionsForEvent);
					}

					transitionsForEvent.AddRange(transitions);
				}
			}

			var immutableCopy = new Dictionary<TEvent, IReadOnlyList<TransitionBase<TStateId>>>();
			foreach ((TEvent trigger, List<TransitionBase<TStateId>> transitions) in transitionsByEvent)
			{
				immutableCopy.Add(trigger, transitions);
			}
			return immutableCopy;
		}

		/// <summary>Returns all added "trigger transitions from any", grouped by their trigger events.</summary>
		/// <remarks>Warning: this is an expensive operation.</remarks>
		public IReadOnlyDictionary<TEvent, IReadOnlyList<TransitionBase<TStateId>>> GetAllTriggerTransitionsFromAny()
		{
			var immutableCopy = new Dictionary<TEvent, IReadOnlyList<TransitionBase<TStateId>>>();
			foreach ((TEvent trigger, List<TransitionBase<TStateId>> transitions) in triggerTransitionsFromAny)
			{
				immutableCopy.Add(trigger, transitions);
			}
			return immutableCopy;
		}

		public override void AcceptVisitor(IStateVisitor visitor)
		{
			visitor.VisitStateMachine(this);
		}
	}

	// Overloaded classes to allow for an easier usage of the StateMachine for common cases.
	// E.g. new StateMachine() instead of new StateMachine<string, string, string>()

	/// <inheritdoc />
	public class StateMachine<TStateId, TEvent> : StateMachine<TStateId, TStateId, TEvent>
	{
		/// <inheritdoc />
		public StateMachine(bool needsExitTime = false, bool isGhostState = false, bool rememberLastState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState, rememberLastState: rememberLastState)
		{
		}
	}

	/// <inheritdoc />
	public class StateMachine<TStateId> : StateMachine<TStateId, TStateId, string>
	{
		public StateMachine(bool needsExitTime = false, bool isGhostState = false, bool rememberLastState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState, rememberLastState: rememberLastState)
		{
		}
	}

	/// <inheritdoc />
	public class StateMachine : StateMachine<string, string, string>
	{
		/// <inheritdoc />
		public StateMachine(bool needsExitTime = false, bool isGhostState = false, bool rememberLastState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState, rememberLastState: rememberLastState)
		{
		}
	}
}