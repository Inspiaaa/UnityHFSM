using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Hierarchichal finite state machine for Unity 
 * by Inspiaaa
 * 
 * Version: 1.6.2
 */

namespace FSM {
	/// <summary>
	/// A finite state machine that can also be used as a state of a parent state machine to create
	/// a hierarchy (-> hierarchical state machine)
	/// </summary>
	public class StateMachine : StateBase {

		private string startState = null;
		private string pendingState = null;

		private StateBase activeState = null;
		private List<TransitionBase> activeTransitions = new List<TransitionBase>();

		public StateBase ActiveState {
			get {
				return activeState;
			}
		}

		public string ActiveStateName {
			get {
				return activeState.name;
			}
		}

		private bool IsRootFSM {
			get {
				return fsm == null;
			}
		}

		// A cached empty list of transitions (For improved readability, less GC)
		private static readonly List<TransitionBase> noTransitions = new List<TransitionBase>();

		private Dictionary<string, StateBase> nameToState 
			= new Dictionary<string, StateBase>();
		private Dictionary<string, List<TransitionBase>> fromNameToTransitions 
			= new Dictionary<string, List<TransitionBase>>();

		private List<TransitionBase> transitionsFromAny = new List<TransitionBase>();

		/// <summary>
		/// Initialises a new instance of the StateMachine class
		/// </summary>
		/// <param name="mono">The MonoBehaviour of the script that created the state machine</param>
		/// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determins whether the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until the active state is ready for a
		/// 	state change (true).</param>
		public StateMachine(MonoBehaviour mono, bool needsExitTime = true) : base(needsExitTime) {
			this.mono = mono;
		}

		/// <summary>
		/// Calls OnEnter if it is the root machine, therefore initialising the state machine
		/// </summary>
		public override void Init()
		{
			if (!IsRootFSM) return;

			OnEnter();
		}

		/// <summary>
		/// Checks that all names of states in transitions / the start state refer 
		/// to existing states and if not, it logs a warning.
		/// It is can be rather helpful and time saving, when you are debugging larger
		/// state machines and want to make sure that all transitions are set up correctly.
		/// </summary>
		public void Validate() {

			// Check that all transitions refer to real states
			foreach (List<TransitionBase> transitions in fromNameToTransitions.Values) {
				foreach (TransitionBase t in transitions) {

					bool fromStateExists = nameToState.ContainsKey(t.from);
					bool toStateExists = nameToState.ContainsKey(t.to);

					if (fromStateExists && toStateExists) continue;

					string errorMessage  = 
						"The '{0}' state that the " + $"{t.GetType()} ('{t.from}' --> '{t.to}') "
						+"is referring to does not exist {1}";

					if (! fromStateExists) {
						Debug.LogWarning(string.Format(errorMessage, 
							"from",
							"and can create an unexpected behaviour later, when the fsm does not transition. "
						));
					}

					if (! toStateExists) {
						Debug.LogWarning(string.Format(errorMessage, 
							"to",
							"and can cause an error later."
						));
					}
				}
			}

			// Check that the start state is an actual state
			if (! nameToState.ContainsKey(startState)) {
				Debug.LogWarning(
					$"The start state '{startState}' does not exist and will "
					+"cause an error. (Typo?)"
				);
			}
		}

		/// <summary>
		/// Notifies the state machine that the state can cleanly exit,
		/// and if a state change is pending, it will execute it.
		/// </summary>
		public void StateCanExit() {
			if (pendingState != null) {
				ChangeState(pendingState);
				pendingState = null;
			}

			if (fsm != null) {
				fsm.StateCanExit();
			}
		}

		public override void RequestExit() {
			if (activeState.needsExitTime) {
				activeState.RequestExit();
				return;	
			}

			if (fsm != null) {
				fsm.StateCanExit();
			}
		}

		/// <summary>
		/// Requests a state change, respecting the <c>needsExitTime</c> property of the active state
		/// </summary>
		/// <param name="name">The name / identifier of the target state</param>
		/// <param name="forceInstantly">Overrides the needsExitTime of the active state if true,
		/// therefore forcing an immediate state change</param>
		public void RequestStateChange(string name, bool forceInstantly = false) {
			if (!activeState.needsExitTime || forceInstantly) {
				ChangeState(name);
			}
			else {
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
		/// Instantly changes to the target state
		/// </summary>
		/// <param name="name">The name / identifier of the active state</param>
		private void ChangeState(string name) {
			StateBase newState = GetState(name);

			if (activeState != null) {
				activeState.OnExit();
			}

			activeState = newState;
			activeState.OnEnter();

			if (fromNameToTransitions.TryGetValue(name, out List<TransitionBase> currentTransitions)) {
				activeTransitions = currentTransitions;

				for (int i = 0; i < activeTransitions.Count; i ++) {
					activeTransitions[i].OnEnter();
				}
			}
			else {
				activeTransitions = noTransitions;
			}
		}

		/// <summary>
		/// Initialises the state machine and must be called before OnLogic is called.
		/// It sets the activeState to the selected startState.
		/// </summary>
		override public void OnEnter() {
			ChangeState(startState);
		}

		public StateBase GetState(string name) {
			if (! nameToState.TryGetValue(name, out StateBase state)) {
				throw new System.Exception(
					$"The state '{name}' has not been defined yet / doesn't exist"
				);
			}

			return state;
		}

		/// <summary>
		/// Checks if a transition can take place, and if this is the case, transition to the
		/// "to" state and return true. Otherwise it returns false
		/// </summary>
		/// <param name="transition"></param>
		/// <returns></returns>
		private bool TryTransition(TransitionBase transition) {
			if (! transition.ShouldTransition())
				return false;
			
			RequestStateChange(transition.to, transition.forceInstantly);

			return true;
		}

		/// <summary>
		/// Runs one logic step. It does at most one transition itself and 
		/// calls the active state's logic function (after the state transition, if
		/// one occurred).
		/// </summary>
		override public void OnLogic() {
			if (activeState == null) {
				throw new System.Exception("The FSM has not been initialised yet! "
					+ "Call fsm.SetStartState(...) and fsm.OnEnter() or fsm.Init() to initialise");
			}

			// Try the "global" transitions that can transition from any state
			for (int i = 0; i < transitionsFromAny.Count; i++) {
				TransitionBase transition = transitionsFromAny[i];

				// Don't transition to the "to" state, if that state is already the active state
				if (transition.to == activeState.name)
					continue;

				if (TryTransition(transition)) {
					activeState.OnLogic();
					return;
				}
			}

			// Try the "normal" transitions that transition from one specific state to another
			for (int i = 0; i < activeTransitions.Count; i++) {
				TransitionBase transition = activeTransitions[i];

				if (TryTransition(transition)) {
					activeState.OnLogic();
					return;
				}
			}

			activeState.OnLogic();
		}

		public override void OnExit() {
			if (activeState != null) {
				activeState.OnExit();
				activeState = null;
			}
		}

		/// <summary>
		/// Adds a new node / state to the state machine
		/// </summary>
		/// <param name="name">The name / identifier of the new state</param>
		/// <param name="state">The new state instance, e.g. <c>State</c>, <c>CoState</c>, <c>StateMachine</c></param>
		public void AddState(string name, StateBase state) {
			state.fsm = this;
			state.name = name;
			state.mono = mono;

			state.Init();

			nameToState[name] = state;

			if (nameToState.Count == 1 && startState == null) {
				SetStartState(name);
			}
		}

		/// <summary>
		/// Defines the entry point of the state machine
		/// </summary>
		/// <param name="name">The name / identifier of the start state</param>
		public void SetStartState(string name) {
			startState = name;
		}

		/// <summary>
		/// Adds a new transition between two states
		/// </summary>
		/// <param name="transition">The transition instance</param>
		public void AddTransition(TransitionBase transition) {
			if (! fromNameToTransitions.ContainsKey(transition.from)) {
				fromNameToTransitions[transition.from] = new List<TransitionBase>();
			}

			transition.fsm = this;
			transition.mono = mono;

			transition.Init();

			fromNameToTransitions[transition.from].Add(transition);
		}

		/// <summary>
		/// Adds a new transition that can happen from any possible state
		/// </summary>
		/// <param name="transition">The transition instance; The "from" field can be
		/// left empty, as it has no meaning in this context.</param>
		public void AddTransitionFromAny(TransitionBase transition) {
			transition.fsm = this;
			transition.mono = mono;

			transition.Init();

			transitionsFromAny.Add(transition);
		}

		public StateMachine this[string name] {
			get {
				if (! nameToState.ContainsKey(name)) {
					throw new System.Exception(
						$"The state '{name}' has not been defined yet / doesn't exist"
					);
				}

				StateBase selectedNode = nameToState[name];

				if (! (selectedNode is StateMachine)) {
					throw new System.Exception(
						$"The state '{name}' is not a StateMachine. "
						+$"To get this state, use fsm.GetState(\"{name}\")."
					);
				}

				return (StateMachine) selectedNode;
			}
		}
	}
}
