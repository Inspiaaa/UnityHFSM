using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
	public class State : FSMNode {
		private Action<State> onEnter;
		private Action<State> onLogic;
		private Action<State> onExit;
		private Func<State, bool> canExit;

		public Timer timer;
		
		public State(Action<State> onEnter = null, 
				Action<State> onLogic = null,
				Action<State> onExit = null,
				Func<State, bool> canExit = null,
				bool needsExitTime = false) : base(needsExitTime) 
		{
			this.onEnter = onEnter;
			this.onLogic = onLogic;
			this.onExit = onExit;
			this.canExit = canExit;

			this.timer = new Timer();
		}

		override public void OnEnter() {
			timer.Reset();

			if (onEnter != null) onEnter(this);
		}

		override public void OnLogic() {
			if (onLogic != null) onLogic(this);
		}

		override public void OnExit() {
			if (onExit != null)	onExit(this);
		}

		override public void RequestExit() {
			if (!needsExitTime || canExit != null && canExit(this)) {
				fsm.StateCanExit();
			}
		}
	}
}