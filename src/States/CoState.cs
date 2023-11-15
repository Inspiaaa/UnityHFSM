using System.Collections;
using UnityEngine;
using System;

namespace UnityHFSM
{
	/// <summary>
	/// A state that can run a Unity coroutine as its OnLogic method.
	/// </summary>
	/// <inheritdoc />
	public class CoState<TStateId, TEvent> : ActionState<TStateId, TEvent>
	{
		private MonoBehaviour mono;

		private Func<IEnumerator> coroutineCreator;
		private Action<CoState<TStateId, TEvent>> onEnter;
		private Action<CoState<TStateId, TEvent>> onExit;
		private Func<CoState<TStateId, TEvent>, bool> canExit;

		private bool shouldLoopCoroutine;

		public ITimer timer;

		private Coroutine activeCoroutine;

		// The CoState class allows you to use either a function without any parameters or a
		// function that takes the state as a parameter to create the coroutine.
		// To allow for this and ease of use, the class has two nearly identical constructors.

		/// <summary>
		/// Initialises a new instance of the CoState class.
		/// </summary>
		/// <param name="mono">The MonoBehaviour of the script that should run the coroutine.</param>
		/// <param name="onEnter">A function that is called when the state machine enters this state.</param>
		/// <param name="coroutine">A coroutine that is run while this state is active.
		/// 	It runs independently from the parent state machine's OnLogic(), because it is handled by Unity.
		/// 	It is started once the state enters and is terminated when the state exits.</param>
		/// <param name="onExit">A function that is called when the state machine exits this state.</param>
		/// <param name="canExit">(Only if needsExitTime is true):
		/// 	Function that determines if the state is ready to exit (true) or not (false).
		/// 	It is called OnExitRequest and on each logic step when a transition is pending.</param>
		/// <param name="loop">If true, it will loop the coroutine, running it again once it has completed.</param>
		/// <inheritdoc cref="StateBase{T}(bool, bool)"/>
		public CoState(
				MonoBehaviour mono,
				Func<CoState<TStateId, TEvent>, IEnumerator> coroutine,
				Action<CoState<TStateId, TEvent>> onEnter = null,
				Action<CoState<TStateId, TEvent>> onExit = null,
				Func<CoState<TStateId, TEvent>, bool> canExit = null,
				bool loop = true,
				bool needsExitTime = false,
				bool isGhostState = false) : base(needsExitTime, isGhostState)
		{
			this.mono = mono;
			this.coroutineCreator = () => coroutine(this);
			this.onEnter = onEnter;
			this.onExit = onExit;
			this.canExit = canExit;
			this.shouldLoopCoroutine = loop;

			timer = new Timer();
		}

		/// <inheritdoc cref="CoState{TStateId, TEvent}(
		/// 	MonoBehaviour,
		/// 	Func{CoState{TStateId, TEvent}, IEnumerator},
		/// 	Action{CoState{TStateId, TEvent}},
		/// 	Action{CoState{TStateId, TEvent}},
		/// 	Func{CoState{TStateId, TEvent}, bool},
		/// 	bool,
		/// 	bool,
		/// 	bool
		/// )"/>
		public CoState(
				MonoBehaviour mono,
				Func<IEnumerator> coroutine,
				Action<CoState<TStateId, TEvent>> onEnter = null,
				Action<CoState<TStateId, TEvent>> onExit = null,
				Func<CoState<TStateId, TEvent>, bool> canExit = null,
				bool loop = true,
				bool needsExitTime = false,
				bool isGhostState = false) : base(needsExitTime, isGhostState)
		{
			this.mono = mono;
			this.coroutineCreator = coroutine;
			this.onEnter = onEnter;
			this.onExit = onExit;
			this.canExit = canExit;
			this.shouldLoopCoroutine = loop;

			timer = new Timer();
		}

		public override void OnEnter()
		{
			timer.Reset();

			onEnter?.Invoke(this);

			if (coroutineCreator != null)
			{
				activeCoroutine = mono.StartCoroutine(
					shouldLoopCoroutine
					? LoopCoroutine()
					: coroutineCreator()
				);
			}
		}

		private IEnumerator LoopCoroutine()
		{
			IEnumerator routine = coroutineCreator();
			while (true)
			{
				// This checks if the routine needs at least one frame to execute.
				// If not, LoopCoroutine will wait 1 frame to avoid an infinite
				// loop which will crash Unity.
				if (routine.MoveNext())
					yield return routine.Current;
				else
					yield return null;

				// Iterate from the onLogic coroutine until it is depleted.
				while (routine.MoveNext())
					yield return routine.Current;

				// Restart the coroutine.
				routine = coroutineCreator();
			}
		}

		public override void OnLogic()
		{
			if (needsExitTime && canExit != null && fsm.HasPendingTransition && canExit(this))
			{
				fsm.StateCanExit();
			}
		}

		public override void OnExit()
		{
			if (activeCoroutine != null)
			{
				mono.StopCoroutine(activeCoroutine);
				activeCoroutine = null;
			}

			onExit?.Invoke(this);
		}

		public override void OnExitRequest()
		{
			if (canExit != null && canExit(this))
			{
				fsm.StateCanExit();
			}
		}
	}

	/// <inheritdoc />
	public class CoState<TStateId> : CoState<TStateId, string>
	{
		/// <inheritdoc />
		public CoState(
			MonoBehaviour mono,
			Func<CoState<TStateId, string>, IEnumerator> coroutine,
			Action<CoState<TStateId, string>> onEnter = null,
			Action<CoState<TStateId, string>> onExit = null,
			Func<CoState<TStateId, string>, bool> canExit = null,
			bool loop = true,
			bool needsExitTime = false,
			bool isGhostState = false)
			: base(
				mono,
				coroutine: coroutine,
				onEnter: onEnter,
				onExit: onExit,
				canExit: canExit,
				loop: loop,
				needsExitTime: needsExitTime,
				isGhostState: isGhostState)
		{
		}

		/// <inheritdoc />
		public CoState(
			MonoBehaviour mono,
			Func<IEnumerator> coroutine,
			Action<CoState<TStateId, string>> onEnter = null,
			Action<CoState<TStateId, string>> onExit = null,
			Func<CoState<TStateId, string>, bool> canExit = null,
			bool loop = true,
			bool needsExitTime = false,
			bool isGhostState = false)
			: base(
				mono,
				coroutine: coroutine,
				onEnter: onEnter,
				onExit: onExit,
				canExit: canExit,
				loop: loop,
				needsExitTime: needsExitTime,
				isGhostState: isGhostState)
		{
		}
	}

	/// <inheritdoc />
	public class CoState : CoState<string, string>
	{
		/// <inheritdoc />
		public CoState(
			MonoBehaviour mono,
			Func<CoState<string, string>, IEnumerator> coroutine,
			Action<CoState<string, string>> onEnter = null,
			Action<CoState<string, string>> onExit = null,
			Func<CoState<string, string>, bool> canExit = null,
			bool loop = true,
			bool needsExitTime = false,
			bool isGhostState = false)
			: base(
				mono,
				coroutine: coroutine,
				onEnter: onEnter,
				onExit: onExit,
				canExit: canExit,
				loop: loop,
				needsExitTime: needsExitTime,
				isGhostState: isGhostState)
		{
		}

		/// <inheritdoc />
		public CoState(
			MonoBehaviour mono,
			Func<IEnumerator> coroutine,
			Action<CoState<string, string>> onEnter = null,
			Action<CoState<string, string>> onExit = null,
			Func<CoState<string, string>, bool> canExit = null,
			bool loop = true,
			bool needsExitTime = false,
			bool isGhostState = false)
			: base(
				mono,
				coroutine: coroutine,
				onEnter: onEnter,
				onExit: onExit,
				canExit: canExit,
				loop: loop,
				needsExitTime: needsExitTime,
				isGhostState: isGhostState)
		{
		}
	}
}
