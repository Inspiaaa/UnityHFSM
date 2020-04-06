using System.Collections;
using System.Collections.Generic;
using UnityEngine;

 // TODO: Add explicit error if fsm has not been initialised yet (calling fsm.OnEnter())

/**
 * * Hierarchichal finite state machine for Unity 
 * by LavaAfterburner
 * 
 * * Version: 1.1
 */

namespace FSM {
	public class StateMachine : FSMNode {
		private string startState;
		private string pendingState;
		public FSMNode activeState;
		private FSMTransition[] activeTransitions;

		private Dictionary<string, FSMNode> states = new Dictionary<string, FSMNode>();
		private Dictionary<string, List<FSMTransition>> transitions = new Dictionary<string, List<FSMTransition>>();

		public StateMachine(MonoBehaviour mono, bool needsExitTime = true) : base(needsExitTime) {
			this.mono = mono;
		}

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

		public void AddState(string name, FSMNode state) {
			state.fsm = this;
			state.name = name;
			state.mono = mono;

			states[name] = state;

			if (states.Count == 1 && startState == null) {
				SetStartState(name);
			}
		}

		public void SetStartState(string name) {
			startState = name;
		}

		public void AddTransition(FSMTransition transition) {
			if (! transitions.ContainsKey(transition.from)) {
				transitions[transition.from] = new List<FSMTransition>();
			}

			transition.fsm = this;
			transition.mono = mono;

			transitions[transition.from].Add(transition);
		}
	}
}