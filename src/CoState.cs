using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace FSM {
	public class CoState : FSMNode
	{
		private Action<CoState> onEnter;
		private Func<CoState, IEnumerator> onLogic;
		private Action<CoState> onExit;
		private Func<CoState, bool> canExit;

		public Timer timer;
		private Coroutine coroutine;

		public CoState(Action<CoState> onEnter = null, 
				Func<CoState, IEnumerator> onLogic = null,
				Action<CoState> onExit = null,
				Func<CoState, bool> canExit = null,
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

			coroutine = null;
		}

		private IEnumerator LoopCoroutine() {
			IEnumerator routine = onLogic(this);
			while (true) {

				// This checks if the routine needs at least one frame to execute.
				// If not, LoopCoroutine will wait 1 frame to avoid an infite loop which will crash Unity
				if (routine.MoveNext())
					yield return routine.Current;
				else
					yield return null;

				while (routine.MoveNext())
					yield return routine.Current;

				routine = onLogic(this);
			}
		}

		override public void OnLogic() {
			if (coroutine == null && onLogic != null) {
				coroutine = mono.StartCoroutine(LoopCoroutine());
			}
		}

		override public void OnExit() {
			mono.StopCoroutine(coroutine);
			coroutine = null;

			if (onExit != null)	onExit(this);
		}

		override public void RequestExit() {
			if (!needsExitTime || canExit != null && canExit(this)) {
				fsm.StateCanExit();
			}
		}
	}
}