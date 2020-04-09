using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 // TODO: Add explicit error if fsm has not been initialised yet (calling fsm.OnEnter())

/**
 * * Hierarchichal finite state machine for Unity 
 * by LavaAfterburner
 * 
 * * Version: 1.2
 */

namespace FSM {
	/// <summary>
	/// A finite state machine
	/// </summary>
	public class StateMachine : FSMNode {
		private string startState;
		private string pendingState;
		public FSMNode activeState;
		private FSMTransition[] activeTransitions;

		private Dictionary<string, FSMNode> states = new Dictionary<string, FSMNode>();
		private Dictionary<string, List<FSMTransition>> transitions = new Dictionary<string, List<FSMTransition>>();

		/// <summary>
		/// Initializes a new instance of the StateMachine class
		/// </summary>
		/// <param name="mono">The MonoBehaviour of the script that created the state machine</param>
		/// <param name="needsExitTime">(Only for hierarchical states):
		/// 	Determins if the state machine as a state of a parent state machine is allowed to instantly
		/// 	exit on a transition (false), or if it should wait until the active state is ready for a
		/// 	state change (true)</param>
		/// <returns></returns>
		public StateMachine(MonoBehaviour mono, bool needsExitTime = true) : base(needsExitTime) {
			this.mono = mono;
		}

		/// <summary>
		/// Notifies the state machine that the state can cleanly exit,
		/// and if a state change is pending, it will execute it
		/// </summary>
		public void StateCanExit() {
			if (pendingState != null) {
				ChangeState(pendingState);
			}

			if (fsm != null) {
				fsm.StateCanExit();
			}
		}

		public override void RequestExit() {
			activeState.RequestExit();
		}

		/// <summary>
		/// Request a state change, respecting the <c>needsExitTime</c> property of the active state
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
            	 * -> state.fsm.StateCanExit()
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
				activeTransitions = transitions[name].ToArray();
			}
			else {
				activeTransitions = new Transition[] {};
			}
		}

		override public void OnEnter() {
			ChangeState(startState);
		}

		override public void OnLogic() {
			foreach(Transition transition in activeTransitions) {
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
		public void AddState(string name, FSMNode state) {
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
		/// Adds a new transition betweeen two states
		/// </summary>
		/// <param name="transition">The transition instance</param>
		public void AddTransition(FSMTransition transition) {
			if (! transitions.ContainsKey(transition.from)) {
				transitions[transition.from] = new List<FSMTransition>();
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

				FSMNode selectedNode = states[name];

				if (selectedNode ! is StateMachine) {
					System.Exception exception = new System.Exception(
						$"The state '{name}' is not a StateMachine"
					);
				}

				return (StateMachine) selectedNode;
			}
		}
	}
}