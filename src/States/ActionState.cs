using System;
using System.Collections.Generic;

namespace UnityHFSM
{
	/// <summary>
	/// Base class of states that support custom actions.
	/// </summary>
	/// <inheritdoc />
	public class ActionState<TStateId, TEvent> : StateBase<TStateId>, IActionable<TEvent>
	{
		// Lazy initialized
		private ActionStorage<TEvent> actionStorage;

		/// <summary>
		/// Initialises a new instance of the ActionState class.
		/// </summary>
		/// <inheritdoc cref="StateBase{T}(bool, bool)"/>
		public ActionState(bool needsExitTime, bool isGhostState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState)
		{
		}

		/// <summary>
		/// Adds an action that can be called with OnAction(). Actions are like the builtin events
		/// OnEnter / OnLogic / ... but are defined by the user.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		/// <param name="action">Function that should be called when the action is run.</param>
		/// <returns>Itself to allow for a fluent interface.</returns>
		public ActionState<TStateId, TEvent> AddAction(TEvent trigger, Action action)
		{
			actionStorage = actionStorage ?? new ActionStorage<TEvent>();
			actionStorage.AddAction(trigger, action);
			return this;
		}

		/// <summary>
		/// Adds an action that can be called with OnAction<T>(). This overload allows you to
		/// run a function that takes one data parameter.
		/// Actions are like the builtin events OnEnter / OnLogic / ... but are defined by the user.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		/// <param name="action">Function that should be called when the action is run.</param>
		/// <typeparam name="TData">Data type of the parameter of the function.</typeparam>
		/// <returns>Itself to allow for a fluent interface.</returns>
		public ActionState<TStateId, TEvent> AddAction<TData>(TEvent trigger, Action<TData> action)
		{
			actionStorage = actionStorage ?? new ActionStorage<TEvent>();
			actionStorage.AddAction(trigger, action);
			return this;
		}

		/// <summary>
		/// Runs an action with the given name.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		public void OnAction(TEvent trigger)
			=> actionStorage?.RunAction(trigger);

		/// <summary>
		/// Runs an action with a given name and lets you pass in one parameter to the action function.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		/// <param name="data">Data to pass as the first parameter to the action.</param>
		/// <typeparam name="TData">Type of the data parameter.</typeparam>
		public void OnAction<TData>(TEvent trigger, TData data)
			=> actionStorage?.RunAction<TData>(trigger, data);
	}

	/// <inheritdoc />
	public class ActionState<TStateId> : ActionState<TStateId, string>
	{
		/// <inheritdoc />
		public ActionState(bool needsExitTime, bool isGhostState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState)
		{
		}
	}

	/// <inheritdoc />
	public class ActionState : ActionState<string, string>
	{
		/// <inheritdoc />
		public ActionState(bool needsExitTime, bool isGhostState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState)
		{
		}
	}
}
