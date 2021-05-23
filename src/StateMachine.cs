using System.Collections.Generic;
using UnityEngine;

/**
 * Hierarchichal finite state machine for Unity 
 * by Inspiaaa
 * 
 * Version: 1.6.4
 */

namespace FSM
{
	/// <summary>
	/// A finite state machine that can also be used as a state of a parent state machine to create
	/// a hierarchy (-> hierarchical state machine)
	/// </summary>
	public class StateMachine : StateBase
	{
		// TODO: Make methods for throwing common errors

		/// <summary>
		/// A bundle of a state together with the outgoing transitions and trigger transitions.
		/// It's useful, as you only need to do one Dictionary lookup for these three items.
		/// </summary>
		private class StateBundle
		{
			// By default, these fields are all null and only get a value assigned when you need it
			// => Lazy evaluation => Memory efficient, when you only need a subset of features
			public StateBase state;
			public List<TransitionBase> transitions;
			public Dictionary<string, List<TransitionBase>> triggerToTransitions;

			public void AddTransition(TransitionBase t)
			{
				if (transitions == null)
				{
					transitions = new List<TransitionBase>();
				}

				transitions.Add(t);
			}

			public void AddTriggerTransition(string trigger, TransitionBase transition) {
				if (triggerToTransitions == null)
				{
					triggerToTransitions = new Dictionary<string, List<TransitionBase>>();
				}

				List<TransitionBase> transitionsOfTrigger;
				
				if (! triggerToTransitions.TryGetValue(trigger, out transitionsOfTrigger))
				{
					transitionsOfTrigger = new List<TransitionBase>();
					triggerToTransitions.Add(trigger, transitionsOfTrigger);
				}

				transitionsOfTrigger.Add(transition);
			}
		}

		// A cached empty list of transitions (For improved readability, less GC)
		private static readonly List<TransitionBase> noTransitions = new List<TransitionBase>();
		private static readonly Dictionary<string, List<TransitionBase>> noTriggerTransitions 
			= new Dictionary<string, List<TransitionBase>>();

		private string startState = null;
		private string pendingState = null;

		private Dictionary<string, StateBundle> nameToStateBundle 
			= new Dictionary<string, StateBundle>();

		private StateBase activeState = null;
		private List<TransitionBase> activeTransitions = noTransitions;
		private Dictionary<string, List<TransitionBase>> activeTriggerTransitions = noTriggerTransitions;

		private List<TransitionBase> transitionsFromAny
			= new List<TransitionBase>();
		private Dictionary<string, List<TransitionBase>> triggerTransitionsFromAny
			= new Dictionary<string, List<TransitionBase>>();

		public StateBase ActiveState => activeState;
		public string ActiveStateName => activeState.name;

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

		// TODO: Maybe re-add Validate()

		/// <summary>
		/// Notifies the state machine that the state can cleanly exit,
		/// and if a state change is pending, it will execute it.
		/// </summary>
		public void StateCanExit()
		{
			if (pendingState != null)
			{
				ChangeState(pendingState);
				pendingState = null;
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
		private void ChangeState(string name)
		{
			if (activeState != null)
			{
				activeState.OnExit();
			}

			// TODO: Throw error when nameToStateBundle[name] does not exist
			StateBundle bundle = nameToStateBundle[name];
			// TODO: Throw error when bundle.state is null => Create a new method for logging errors
			activeState = bundle.state;
			activeState.OnEnter();

			activeTransitions = bundle.transitions;
			if (activeTransitions == null)
			{
				activeTransitions = noTransitions;
			}

			activeTriggerTransitions = bundle.triggerToTransitions;
			if (activeTriggerTransitions == null)
			{
				activeTriggerTransitions = noTriggerTransitions;
			}
		}

		/// <summary>
		/// Requests a state change, respecting the <c>needsExitTime</c> property of the active state
		/// </summary>
		/// <param name="name">The name / identifier of the target state</param>
		/// <param name="forceInstantly">Overrides the needsExitTime of the active state if true,
		/// therefore forcing an immediate state change</param>
		public void RequestStateChange(string name, bool forceInstantly = false)
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
		/// "to" state and return true. Otherwise it returns false
		/// </summary>
		/// <param name="transition"></param>
		/// <returns></returns>
		private bool TryTransition(TransitionBase transition)
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
		public void SetStartState(string name)
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
				throw new System.Exception("The FSM has not been initialised yet! "
					+ "Call fsm.SetStartState(...) and fsm.OnEnter() or fsm.Init() to initialise");
			}

			// Try the "global" transitions that can transition from any state
			for (int i = 0; i < transitionsFromAny.Count; i++)
			{
				TransitionBase transition = transitionsFromAny[i];

				// Don't transition to the "to" state, if that state is already the active state
				if (transition.to == activeState.name)
					continue;

				if (TryTransition(transition))
					break;
			}

			// Try the "normal" transitions that transition from one specific state to another
			for (int i = 0; i < activeTransitions.Count; i++)
			{
				TransitionBase transition = activeTransitions[i];

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
				activeState = null;
			}
		}

		private StateBundle GetOrCreateStateBundle(string name) {
			StateBundle bundle;

			if (! nameToStateBundle.TryGetValue(name, out bundle)) {
				bundle = new StateBundle();
				nameToStateBundle.Add(name, bundle);
			}

			return bundle;
		}

		/// <summary>
		/// Adds a new node / state to the state machine
		/// </summary>
		/// <param name="name">The name / identifier of the new state</param>
		/// <param name="state">The new state instance, e.g. <c>State</c>, <c>CoState</c>, <c>StateMachine</c></param>
		public void AddState(string name, StateBase state)
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

		private void InitTransition(TransitionBase transition)
		{
			transition.fsm = this;
			transition.mono = mono;

			transition.Init();
		}

		/// <summary>
		/// Adds a new transition between two states
		/// </summary>
		/// <param name="transition">The transition instance</param>
		public void AddTransition(TransitionBase transition)
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
		public void AddTransitionFromAny(TransitionBase transition)
		{
			InitTransition(transition);

			transitionsFromAny.Add(transition);
		}

		public void AddTriggerTransition(string trigger, TransitionBase transition)
		{
			InitTransition(transition);

			StateBundle bundle = GetOrCreateStateBundle(transition.from);
			bundle.AddTriggerTransition(trigger, transition);
		}

		public void AddTriggerTransitionFromAny(string trigger, TransitionBase transition)
		{
			InitTransition(transition);

			List<TransitionBase> transitionsOfTrigger;

			if (!triggerTransitionsFromAny.TryGetValue(trigger, out transitionsOfTrigger)) {
				transitionsOfTrigger = new List<TransitionBase>();
				triggerTransitionsFromAny.Add(trigger, transitionsOfTrigger);
			}

			transitionsOfTrigger.Add(transition);
		}

		public void Trigger(string trigger)
		{
			if (activeState == null)
			{
				throw new System.Exception("The FSM has not been initialised yet! "
					+ "Call fsm.SetStartState(...) and fsm.OnEnter() or fsm.Init() to initialise");
			}

			List<TransitionBase> triggerTransitions;

			if (triggerTransitionsFromAny.TryGetValue(trigger, out triggerTransitions))
			{
				for (int i = 0; i < triggerTransitions.Count; i ++)
				{
					TransitionBase transition = triggerTransitions[i];

					if (transition.to == activeState.name)
						continue;
					
					if (TryTransition(transition))
						return;
				}
			}

			for (int i = 0; i < activeTriggerTransitions.Count; i ++)
			{
				TransitionBase transition = activeTriggerTransitions[i];
				
				if (TryTransition(transition))
					return;
			}
			
		}

		public StateBase GetState(string name)
		{
			StateBundle bundle;

			if (!nameToStateBundle.TryGetValue(name, out bundle) || bundle.state == null)
			{
				throw new System.Exception(
					$"The state '{name}' has not been defined yet / doesn't exist"
				);
			}

			return bundle.state;
		}

		public StateMachine this[string name]
		{
			get
			{
				StateBase state = GetState(name);

				if (!(state is StateMachine))
				{
					throw new System.Exception(
						$"The state '{name}' is not a StateMachine. "
						+ $"To get this state, use fsm.GetState(\"{name}\")."
					);
				}

				return (StateMachine)state;
			}
		}
	}
}
