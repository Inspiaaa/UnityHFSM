using System;
using System.Collections.Generic;

namespace FSM
{
	public class ActionableState<TStateId, TEvent> : StateBase<TStateId>, IActionable<TEvent>
	{
		// Lazy initialized
		private Dictionary<TEvent, Delegate> actionsByEvent;

		public ActionableState(bool needsExitTime) : base(needsExitTime: needsExitTime)
		{
		}

		private void AddGenericAction(TEvent trigger, Delegate action)
		{
			actionsByEvent = actionsByEvent ?? new Dictionary<TEvent, Delegate>();
			actionsByEvent[trigger] = action;
		}

		public ActionableState<TStateId, TEvent> AddAction(TEvent trigger, Action action)
		{
			AddGenericAction(trigger, action);
			return this;
		}

		public ActionableState<TStateId, TEvent> AddAction<TData>(TEvent trigger, Action<TData> action)
		{
			AddGenericAction(trigger, action);
			return this;
		}

		public void OnAction(TEvent trigger)
		{
			Delegate action = null;
			actionsByEvent?.TryGetValue(trigger, out action);
			((Action)action)?.Invoke();
		}

		public void OnAction<TData>(TEvent trigger, TData data)
		{
			Delegate action = null;
			actionsByEvent?.TryGetValue(trigger, out action);
			((Action<TData>)action)?.Invoke(data);
		}
	}

	public class ActionableState : ActionableState<string, string>
	{
		public ActionableState(bool needsExitTime) : base(needsExitTime: needsExitTime)
		{
		}
	}
}
