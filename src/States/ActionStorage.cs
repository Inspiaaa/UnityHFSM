using System;
using System.Collections.Generic;
using UnityHFSM.Exceptions;

namespace UnityHFSM
{
	/// <summary>
	/// Class that can store and run actions.
	/// It makes implementing an action system easier in the various state classes.
	/// </summary>
	public class ActionStorage<TEvent>
	{
		private Dictionary<TEvent, Delegate> actionsByEvent = new Dictionary<TEvent, Delegate>();

		/// <summary>
		/// Returns the action belonging to the specified event.
		/// If it does not exist null is returned.
		/// If the type of the existing action does not match the desired type an exception
		/// is thrown.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		/// <typeparam name="TTarget">Type of the function (delegate) belonging to the action.</typeparam>
		/// <returns>The action with the specified name.</returns>
		private TTarget TryGetAndCastAction<TTarget>(TEvent trigger) where TTarget : Delegate
		{
			Delegate action = null;
			actionsByEvent.TryGetValue(trigger, out action);

			if (action is null)
			{
				return null;
			}

			TTarget target = action as TTarget;

			if (target is null)
			{
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
		/// <param name="trigger">Name of the action.</param>
		/// <param name="action">Function that should be called when the action is run.</param>
		public void AddAction(TEvent trigger, Action action)
		{
			actionsByEvent[trigger] = action;
		}

		/// <summary>
		/// Adds an action that can be called with RunAction<T>(). This overload allows you to
		/// run a function that takes one data parameter.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		/// <param name="action">Function that should be called when the action is run.</param>
		/// <typeparam name="TData">Data type of the parameter of the function.</typeparam>
		public void AddAction<TData>(TEvent trigger, Action<TData> action)
		{
			actionsByEvent[trigger] = action;
		}

		/// <summary>
		/// Runs an action with the given name.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		public void RunAction(TEvent trigger)
			=> TryGetAndCastAction<Action>(trigger)?.Invoke();

		/// <summary>
		/// Runs an action with a given name and lets you pass in one parameter to the action function.
		/// If the action is not defined / hasn't been added, nothing will happen.
		/// </summary>
		/// <param name="trigger">Name of the action.</param>
		/// <param name="data">Data to pass as the first parameter to the action.</param>
		/// <typeparam name="TData">Type of the data parameter.</typeparam>
		public void RunAction<TData>(TEvent trigger, TData data)
			=> TryGetAndCastAction<Action<TData>>(trigger)?.Invoke(data);
	}
}
