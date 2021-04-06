using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/**
 * Hierarchichal finite state machine for Unity 
 * by LavaAfterburner
 * 
 * Version: 1.4.0
 */

namespace FSM {
	/// <summary>
	/// A finite state machine
	/// </summary>
	public class StateMachine : StateBase {

		private string startState;
		private string pendingState;

		private StateBase activeState;
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

		// A cached empty list of transitions (For improved readability, less GC)
		private static readonly List<TransitionBase> noTransitions = new List<TransitionBase>();

		private Dictionary<string, StateBase> states = new Dictionary<string, StateBase>();
		private Dictionary<string, List<TransitionBase>> transitions = new Dictionary<string, List<TransitionBase>>();

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
			activeState.RequestExit();
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
			if (! states.ContainsKey(name)) {
				System.Exception exception = new System.Exception(
					$"The state '{name}' has not been defined yet / doesn't exist"
				);
			}

			if (activeState != null) {
				activeState.OnExit();
			}

			activeState = states[name];
			activeState.OnEnter();

			if (transitions.ContainsKey(name)) {
				activeTransitions = transitions[name];

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
		/// OnEnter() sets the activeState to the selected startState.
		/// </summary>
		override public void OnEnter() {
			ChangeState(startState);
		}

		override public void OnLogic() {
			if (activeState == null) {
				throw new System.Exception("The FSM has not been initialised yet! "
					+ "Call fsm.SetStartState(...) and fsm.OnEnter() to initialise");
			}

			for (int i = 0; i < activeTransitions.Count; i++) {
				TransitionBase transition = activeTransitions[i];

				if (! transition.ShouldTransition())
					continue;
				
				if (! activeState.needsExitTime || transition.forceInstantly) {
					ChangeState(transition.to);
				}
				else {
					RequestStateChange(transition.to);
				}

				break;
			}
			
			activeState.OnLogic();
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

			states[name] = state;

			if (states.Count == 1 && startState == null) {
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
			if (! transitions.ContainsKey(transition.from)) {
				transitions[transition.from] = new List<TransitionBase>();
			}

			transition.fsm = this;
			transition.mono = mono;

			transitions[transition.from].Add(transition);
		}

		public StateMachine this[string name] {
			get {
				if (! states.ContainsKey(name)) {
					System.Exception exception = new System.Exception(
						$"The state '{name}' has not been defined yet / doesn't exist"
					);
				}

				StateBase selectedNode = states[name];

				if (! (selectedNode is StateMachine)) {
					System.Exception exception = new System.Exception(
						$"The state '{name}' is not a StateMachine"
					);
				}

				return (StateMachine) selectedNode;
			}
		}
	}
}
