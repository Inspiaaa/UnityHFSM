using System.Collections;
using UnityEngine;
using System;

namespace FSM
{
	/// <summary>
	/// A state that can run a Unity coroutine as its OnLogic method
	/// </summary>
	public class CoState<TEvent> : StateBase<TEvent>
	{
		private Action<CoState<TEvent>> onEnter;
		private Func<CoState<TEvent>, IEnumerator> onLogic;
		private Action<CoState<TEvent>> onExit;
		private Func<CoState<TEvent>, bool> canExit;

		public Timer timer;
		private Coroutine coroutine;

		/// <summary>
		/// Initialises a new instance of the CoState class
		/// </summary>
		/// <param name="onEnter">A function that is called when the state machine enters this state</param>
		/// <param name="onLogic">A coroutine that is run while this state is active
		/// 	It runs independently from the parent state machine's OnLogic(), because it is handled by Unity.
		/// 	It is run again once it has completed.
		/// 	It is terminated when the state exits.</param>
		/// <param name="onExit">A function that is called when the state machine exits this state</param>
		/// <param name="canExit">(Only if needsExitTime is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, later in OnLogic() it should call fsm.StateCanExit().</param>
		/// <param name="needsExitTime">Determins if the state is allowed to instantly
		/// exit on a transition (false), or if the state machine should wait until the state is ready for a
		/// state change (true)</param>
		public CoState(
				Action<CoState<TEvent>> onEnter = null,
				Func<CoState<TEvent>, IEnumerator> onLogic = null,
				Action<CoState<TEvent>> onExit = null,
				Func<CoState<TEvent>, bool> canExit = null,
				bool needsExitTime = false) : base(needsExitTime)
		{
			this.onEnter = onEnter;
			this.onLogic = onLogic;
			this.onExit = onExit;
			this.canExit = canExit;

			this.timer = new Timer();
		}

		override public void OnEnter()
		{
			timer.Reset();

			onEnter?.Invoke(this);

			coroutine = null;
		}

		private IEnumerator LoopCoroutine()
		{
			IEnumerator routine = onLogic(this);
			while (true)
			{

				// This checks if the routine needs at least one frame to execute.
				// If not, LoopCoroutine will wait 1 frame to avoid an infinite 
				// loop which will crash Unity
				if (routine.MoveNext())
					yield return routine.Current;
				else
					yield return null;

				// Iterate from the onLogic coroutine until it is depleted
				while (routine.MoveNext())
					yield return routine.Current;

				// Restart the onLogic coroutine
				routine = onLogic(this);
			}
		}

		public override void OnLogic()
		{
			if (coroutine == null && onLogic != null)
			{
				coroutine = mono.StartCoroutine(LoopCoroutine());
			}
		}

		public override void OnExit()
		{
			if (coroutine != null)
			{
				mono.StopCoroutine(coroutine);
				coroutine = null;
			}

			onExit?.Invoke(this);
		}

		public override void RequestExit()
		{
			if (!needsExitTime || (canExit != null && canExit(this)))
			{
				fsm.StateCanExit();
			}
		}
	}

	public class CoState : CoState<string>
	{
	}
}
