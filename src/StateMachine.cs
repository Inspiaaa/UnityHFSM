using System.Collections.Generic;
using UnityEngine;

/**
 * Hierarchichal finite state machine for Unity 
 * by Inspiaaa
 * 
 * Version: 1.7.2
 */

namespace FSM
{
	/// <summary>
	/// A finite state machine that can also be used as a state of a parent state machine to create
	/// a hierarchy (-> hierarchical state machine)
	/// </summary>
	public class StateMachine<TEvent> : StateBase<TEvent>, ITriggerable<TEvent>
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
			public StateBase<TEvent> state;
			public List<TransitionBase<TEvent>> transitions;
			public Dictionary<TEvent, List<TransitionBase<TEvent>>> triggerToTransitions;

			public void AddTransition(TransitionBase<TEvent> t)
			{
				if (transitions == null)
				{
					transitions = new List<TransitionBase<TEvent>>();
				}

				transitions.Add(t);
			}

			public void AddTriggerTransition(TEvent trigger, TransitionBase<TEvent> transition) {
				if (triggerToTransitions == null)
				{
					triggerToTransitions = new Dictionary<TEvent, List<TransitionBase<TEvent>>>();
				}

				List<TransitionBase<TEvent>> transitionsOfTrigger;
				
				if (! triggerToTransitions.TryGetValue(trigger, out transitionsOfTrigger))
				{
					transitionsOfTrigger = new List<TransitionBase<TEvent>>();
					triggerToTransitions.Add(trigger, transitionsOfTrigger);
				}

				transitionsOfTrigger.Add(transition);
			}
		}

		// A cached empty list of transitions (For improved readability, less GC)
		private static readonly List<TransitionBase<TEvent>> noTransitions = new List<TransitionBase<TEvent>>(0);
		private static readonly Dictionary<TEvent, List<TransitionBase<TEvent>>> noTriggerTransitions
			= new Dictionary<TEvent, List<TransitionBase<TEvent>>>(0);

		private TEvent startState = default;
		private TEvent pendingState = default;

		private Dictionary<TEvent, StateBundle> nameToStateBundle
			= new Dictionary<TEvent, StateBundle>();

		private StateBase<TEvent> activeState = null;
		private List<TransitionBase<TEvent>> activeTransitions = noTransitions;
		private Dictionary<TEvent, List<TransitionBase<TEvent>>> activeTriggerTransitions = noTriggerTransitions;

		private List<TransitionBase<TEvent>> transitionsFromAny
			= new List<TransitionBase<TEvent>>();
		private Dictionary<TEvent, List<TransitionBase<TEvent>>> triggerTransitionsFromAny
			= new Dictionary<TEvent, List<TransitionBase<TEvent>>>();

		public StateBase<TEvent> ActiveState
		{
			get
			{
				if (activeState == null)
				{
					throw new FSM.Exceptions.StateMachineNotInitializedException(
						"Trying to get the active state"
					);
				}

				return activeState;
			}
		}
		public TEvent ActiveStateName => ActiveState.name;

		private bool IsRootFsm => fsm == null;

		/// <summary>
		/// Initialises a new instance of the StateMachine class
		/// </summary>
		/// <param name="mono">The MonoBehaviour of the script that created the state machine</param>
		/// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determins whether the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until the active state is ready for a
		/// 	state change (true).</param>
		public StateMachine(MonoBehaviour mono, bool needsExitTime = true) : base(needsExitTime)
		{
			this.mono = mono;
		}

		/// <summary>
		/// Notifies the state machine that the state can cleanly exit,
		/// and if a state change is pending, it will execute it.
		/// </summary>
		public void StateCanExit()
		{
			if (pendingState != null)
			{
				ChangeState(pendingState);
				pendingState = default;
			}

			if (fsm != null)
			{
				fsm.StateCanExit();
			}
		}

		public override void RequestExit()
		{
			if (activeState.needsExitTime)
			{
				activeState.RequestExit();
				return;
			}

			if (fsm != null)
			{
				fsm.StateCanExit();
			}
		}

		/// <summary>
		/// Instantly changes to the target state
		/// </summary>
		/// <param name="name">The name / identifier of the active state</param>
		private void ChangeState(TEvent name)
		{
			if (activeState != null)
			{
				activeState.OnExit();
			}

			StateBundle bundle;

			if (!nameToStateBundle.TryGetValue(name, out bundle) || bundle.state == null)
			{
				throw new FSM.Exceptions.StateNotFoundException<TEvent>(name, "Switching states");
			}

			activeState = bundle.state;
			activeState.OnEnter();

			activeTransitions = bundle.transitions;
			if (activeTransitions == null)
			{
				activeTransitions = noTransitions;
			}
			else
			{
				for (int i = 0; i < activeTransitions.Count; i ++)
				{
					activeTransitions[i].OnEnter();
				}
			}

			activeTriggerTransitions = bundle.triggerToTransitions;
			if (activeTriggerTransitions == null)
			{
				activeTriggerTransitions = noTriggerTransitions;
			}
			else
			{
				foreach (List<TransitionBase<TEvent>> transitions in activeTriggerTransitions.Values)
				{
					for (int i = 0; i < transitions.Count; i ++)
					{
						transitions[i].OnEnter();
					}
				}
			}
		}

		/// <summary>
		/// Requests a state change, respecting the <c>needsExitTime</c> property of the active state
		/// </summary>
		/// <param name="name">The name / identifier of the target state</param>
		/// <param name="forceInstantly">Overrides the needsExitTime of the active state if true,
		/// therefore forcing an immediate state change</param>
		public void RequestStateChange(TEvent name, bool forceInstantly = false)
		{
			if (!activeState.needsExitTime || forceInstantly)
			{
				ChangeState(name);
			}
			else
			{
				pendingState = name;
				activeState.RequestExit();
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
		private bool TryTransition(TransitionBase<TEvent> transition)
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
		public void SetStartState(TEvent name)
		{
			startState = name;
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
			ChangeState(startState);

			for (int i = 0; i < transitionsFromAny.Count; i ++)
			{
				transitionsFromAny[i].OnEnter();
			}

			foreach (List<TransitionBase<TEvent>> transitions in triggerTransitionsFromAny.Values)
			{
				for (int i = 0; i < transitions.Count; i ++)
				{
					transitions[i].OnEnter();
				}
			}
		}

		/// <summary>
		/// Runs one logic step. It does at most one transition itself and 
		/// calls the active state's logic function (after the state transition, if
		/// one occurred).
		/// </summary>
		public override void OnLogic()
		{
			if (activeState == null)
			{
				throw new FSM.Exceptions.StateMachineNotInitializedException(
					"Running OnLogic"
				);
			}

			// Try the "global" transitions that can transition from any state
			for (int i = 0; i < transitionsFromAny.Count; i++)
			{
				TransitionBase<TEvent> transition = transitionsFromAny[i];

				// Don't transition to the "to" state, if that state is already the active state
				if (transition.to.Equals(activeState.name))
					continue;

				if (TryTransition(transition))
					break;
			}

			// Try the "normal" transitions that transition from one specific state to another
			for (int i = 0; i < activeTransitions.Count; i++)
			{
				TransitionBase<TEvent> transition = activeTransitions[i];

				if (TryTransition(transition))
					break;
			}

			activeState.OnLogic();
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
		private StateBundle GetOrCreateStateBundle(TEvent name) {
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
		public void AddState(TEvent name, StateBase<TEvent> state)
		{
			state.fsm = this;
			state.name = name;
			state.mono = mono;

			state.Init();

			StateBundle bundle = GetOrCreateStateBundle(name);
			bundle.state = state;

			if (nameToStateBundle.Count == 1 && startState == null)
			{
				SetStartState(name);
			}
		}

		/// <summary>
		/// Initialises a transition, i.e. sets its fields, like mono and fsm, and then calls its Init method.
		/// </summary>
		/// <param name="transition"></param>
		private void InitTransition(TransitionBase<TEvent> transition)
		{
			transition.fsm = this;
			transition.mono = mono;

			transition.Init();
		}

		/// <summary>
		/// Adds a new transition between two states.
		/// </summary>
		/// <param name="transition">The transition instance</param>
		public void AddTransition(TransitionBase<TEvent> transition)
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
		public void AddTransitionFromAny(TransitionBase<TEvent> transition)
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
		public void AddTriggerTransition(TEvent trigger, TransitionBase<TEvent> transition)
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
		public void AddTriggerTransitionFromAny(TEvent trigger, TransitionBase<TEvent> transition)
		{
			InitTransition(transition);

			List<TransitionBase<TEvent>> transitionsOfTrigger;

			if (!triggerTransitionsFromAny.TryGetValue(trigger, out transitionsOfTrigger)) {
				transitionsOfTrigger = new List<TransitionBase<TEvent>>();
				triggerTransitionsFromAny.Add(trigger, transitionsOfTrigger);
			}

			transitionsOfTrigger.Add(transition);
		}

		/// <summary>
		/// Activates the specified trigger, checking all targeted trigger transitions to see whether
		/// a transition should occur.
		/// </summary>
		/// <param name="trigger">The name / identifier of the trigger</param>
		/// <returns>True when a transition occurred, otherwise false</returns>
		private bool TryTrigger(TEvent trigger)
		{
			if (activeState == null)
			{
				throw new FSM.Exceptions.StateMachineNotInitializedException(
					"Checking all trigger transitions of the active state"
				);
			}

			List<TransitionBase<TEvent>> triggerTransitions;

			if (triggerTransitionsFromAny.TryGetValue(trigger, out triggerTransitions))
			{
				for (int i = 0; i < triggerTransitions.Count; i ++)
				{
					TransitionBase<TEvent> transition = triggerTransitions[i];

					if (transition.to.Equals(activeState.name))
						continue;
					
					if (TryTransition(transition))
						return true;
				}
			}

			triggerTransitions = activeTriggerTransitions[trigger];
			
			for (int i = 0; i < triggerTransitions.Count; i ++)
			{
				TransitionBase<TEvent> transition = triggerTransitions[i];
				
				if (TryTransition(transition))
					return true;
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

		public StateBase<TEvent> GetState(TEvent name)
		{
			StateBundle bundle;

			if (!nameToStateBundle.TryGetValue(name, out bundle) || bundle.state == null)
			{
				throw new FSM.Exceptions.StateNotFoundException<TEvent>(name, "Getting a state");
			}

			return bundle.state;
		}

		public StateMachine<TEvent> this[TEvent name]
		{
			get
			{
				StateBase<TEvent> state = GetState(name);

				if (!(state is StateMachine<TEvent>))
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

				return (StateMachine<TEvent>)state;
			}
		}
	}

	public class StateMachine : StateMachine<string>
	{
		public StateMachine(MonoBehaviour mono, bool needsExitTime = true) : base(mono, needsExitTime)
		{
		}
	}
}
