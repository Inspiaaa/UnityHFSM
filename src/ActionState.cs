using System;
using System.Collections.Generic;
using FSM.Exceptions;

namespace FSM
{
	public class ActionState<TStateId, TEvent> : StateBase<TStateId>, IActionable<TEvent>
	{
		// Lazy initialized
		private Dictionary<TEvent, Delegate> actionsByEvent;

		public ActionState(bool needsExitTime) : base(needsExitTime: needsExitTime)
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

		// Fluent interface
		public ActionState<TStateId, TEvent> AddAction(TEvent trigger, Action action)
		{
			AddGenericAction(trigger, action);
			return this;
		}

		public ActionState<TStateId, TEvent> AddAction<TData>(TEvent trigger, Action<TData> action)
		{
			AddGenericAction(trigger, action);
			return this;
		}

		public void OnAction(TEvent trigger)
			=> TryGetAndCastAction<Action>(trigger)?.Invoke();

		public void OnAction<TData>(TEvent trigger, TData data)
			=> TryGetAndCastAction<Action<TData>>(trigger)?.Invoke(data);
	}

	public class ActionState : ActionState<string, string>
	{
		public ActionState(bool needsExitTime) : base(needsExitTime: needsExitTime)
		{
		}
	}
}
