using System;
using System.Collections.Generic;
using FSM.Exceptions;

namespace FSM
{
	/// <summary>
	/// Base class of states that should support custom actions.
	/// </summary>
	public class ActionState<TStateId, TEvent> : StateBase<TStateId>, IActionable<TEvent>
	{
		// Lazy initialized
		private Dictionary<TEvent, Delegate> actionsByEvent;

		public ActionState(bool needsExitTime, bool isGhostState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState)
		{
		}

		private void AddGenericAction(TEvent trigger, Delegate action)
		{
			actionsByEvent = actionsByEvent ?? new Dictionary<TEvent, Delegate>();
			actionsByEvent[trigger] = action;
		}

		private TTarget TryGetAndCastAction<TTarget>(TEvent trigger) where TTarget : Delegate {
			Delegate action = null;
			actionsByEvent?.TryGetValue(trigger, out action);

			if (action is null) {
				return null;
			}

			TTarget target = action as TTarget;

			if (target is null) {
				throw new InvalidOperationException(ExceptionFormatter.Format(
					context: $"Trying to call the action '{trigger}'.",
					problem: $"The expected argument type ({typeof(TTarget)}) does not match the "
						+ $"type of the added action ({action}).",
					solution: "Check that the type of action that was added matches the type of action that is called. \n"
						+ "E.g. AddAction<int>(...) => OnAction<int>(...) \n"
						+ "E.g. AddAction(...) => OnAction(...) \n"
						+ "E.g. NOT: AddAction<int>(...) => OnAction<bool>(...)"
				));
			}

			return target;
		}

		/// <summary>
		/// Adds an action that can be called with OnAction(). Actions are like the builtin events
		/// OnEnter / OnLogic / ... but are defined by the user.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="action">Function that should be called when the action is run</param>
		/// <returns>Itself</returns>
		public ActionState<TStateId, TEvent> AddAction(TEvent trigger, Action action)
		{
			AddGenericAction(trigger, action);
			// Fluent interface
			return this;
		}

		/// <summary>
		/// Adds an action that can be called with OnAction<T>(). This overload allows you to
		/// run a function that takes one data parameter.
		/// Actions are like the builtin events OnEnter / OnLogic / ... but are defined by the user.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="action">Function that should be called when the action is run</param>
		/// <typeparam name="TData">Data type of the parameter of the function</typeparam>
		/// <returns>Itself</returns>
		public ActionState<TStateId, TEvent> AddAction<TData>(TEvent trigger, Action<TData> action)
		{
			AddGenericAction(trigger, action);
			// Fluent interface
			return this;
		}

		/// <summary>
		/// Runs an action with the given name.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		public void OnAction(TEvent trigger)
			=> TryGetAndCastAction<Action>(trigger)?.Invoke();

		/// <summary>
		/// Runs an action with a given name and lets you pass in one parameter to the action function.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action</param>
		/// <param name="data">Data to pass as the first parameter to the action</param>
		/// <typeparam name="TData">Type of the data parameter</typeparam>
		public void OnAction<TData>(TEvent trigger, TData data)
			=> TryGetAndCastAction<Action<TData>>(trigger)?.Invoke(data);
	}

	public class ActionState<TStateId> : ActionState<TStateId, string>
	{
		public ActionState(bool needsExitTime, bool isGhostState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState)
		{
		}
	}

	public class ActionState : ActionState<string, string>
	{
		public ActionState(bool needsExitTime, bool isGhostState = false)
			: base(needsExitTime: needsExitTime, isGhostState: isGhostState)
		{
		}
	}
}
