
using NUnit.Framework;
using System.Collections.Generic;
using System.Text;
using UnityHFSM;

namespace UnityHFSM.Tests
{
	public class Recorder<TStateId>
	{
		private enum StateAction
		{
			ENTER, LOGIC, EXIT
		}

		private enum TransitionAction
		{
			ENTER, SHOULD_TRANSITION
		}

		private abstract class Event
		{
			public abstract bool Equals(Event other);
		}

		private class StateEvent : Event
		{
			public TStateId state;
			public StateAction action;

			public StateEvent(TStateId state, StateAction action)
			{
				this.state = state;
				this.action = action;
			}

			public override bool Equals(Event other)
			{
				if (!(other is StateEvent e))
				{
					return false;
				}
				return state.Equals(e.state) && action == e.action;
			}

			public override string ToString() => $"({state}, {action})";
		}

		private class TransitionEvent : Event
		{
			public TStateId from, to;
			public TransitionAction action;

			public TransitionEvent(TStateId from, TStateId to, TransitionAction action)
			{
				this.from = from;
				this.to = to;
				this.action = action;
			}

			public override bool Equals(Event other)
			{
				if (!(other is TransitionEvent e))
				{
					return false;
				}
				return from.Equals(e.from)
					&& to.Equals(e.to)
					&& action == e.action;
			}

			public override string ToString() => $"({from}->{to}, {action})";
		}

		private class CustomEvent : Event
		{
			public string message;

			public CustomEvent(string message)
			{
				this.message = message;
			}

			public override bool Equals(Event other)
			{
				if (!(other is CustomEvent e))
				{
					return false;
				}
				return this.message == e.message;
			}

			public override string ToString() => $"({message})";
		}

		public class RecorderQuery
		{
			private Recorder<TStateId> recorder;

			public RecorderQuery(Recorder<TStateId> recorder)
			{
				this.recorder = recorder;
			}

			private void CheckNext(Event expectedEvent)
			{
				if (recorder.recordedEvents.Count == 0)
				{
					Assert.Fail($"No recorded steps left. {expectedEvent} has not happened yet.");
				}

				Event nextEvent = recorder.recordedEvents.Dequeue();
				bool doesNextEventMatch = expectedEvent.Equals(nextEvent);
				if (!doesNextEventMatch)
				{
					Assert.Fail(
						$"Next event {nextEvent} does not match the expected event {expectedEvent}.\n"
						+ "Remaining steps: " + recorder.CreateTraceback());
				}
			}

			public RecorderQuery Enter(TStateId stateName)
			{
				CheckNext(new StateEvent(stateName, StateAction.ENTER));
				return this;
			}

			public RecorderQuery Logic(TStateId stateName)
			{
				CheckNext(new StateEvent(stateName, StateAction.LOGIC));
				return this;
			}

			public RecorderQuery Exit(TStateId stateName)
			{
				CheckNext(new StateEvent(stateName, StateAction.EXIT));
				return this;
			}

			public RecorderQuery TransitionEnter(TStateId from, TStateId to)
			{
				CheckNext(new TransitionEvent(from, to, TransitionAction.ENTER));
				return this;
			}

			public RecorderQuery ShouldTransition(TStateId from, TStateId to)
			{
				CheckNext(new TransitionEvent(from, to, TransitionAction.SHOULD_TRANSITION));
				return this;
			}

			public RecorderQuery Custom(string message)
			{
				CheckNext(new CustomEvent(message));
				return this;
			}

			public void All()
			{
				if (recorder.recordedEvents.Count != 0)
				{
					Assert.Fail($"Too many events happened. Remaining steps: " + recorder.CreateTraceback());
				}
			}

			public void Empty()
			{
				if (recorder.recordedEvents.Count != 0)
				{
					Assert.Fail("Expected nothing to happen. Recorded steps: " + recorder.CreateTraceback());
				}
			}
		}

		private Queue<Event> recordedEvents;
		private StateDecorator<TStateId, string> tracker;

		// Fluent interface for checking the validity of the recorded events.
		public RecorderQuery Expect => new RecorderQuery(this);

		// Creates a new StateBase whose OnEnter / OnLogic / OnExit events are tracked.
		public StateBase<TStateId> TrackedState => Track(new StateBase<TStateId>(false));

		public Recorder()
		{
			recordedEvents = new Queue<Event>();
			tracker = new StateDecorator<TStateId, string>(
				beforeOnEnter: s => RecordEnter(s.name),
				beforeOnLogic: s => RecordLogic(s.name),
				beforeOnExit: s => RecordExit(s.name)
			);
		}

		public void RecordEnter(TStateId state)
			=> recordedEvents.Enqueue(new StateEvent(state, StateAction.ENTER));
		public void RecordLogic(TStateId state)
			=> recordedEvents.Enqueue(new StateEvent(state, StateAction.LOGIC));
		public void RecordExit(TStateId state)
			=> recordedEvents.Enqueue(new StateEvent(state, StateAction.EXIT));

		public void RecordTransitionEnter(TStateId from, TStateId to)
			=> recordedEvents.Enqueue(new TransitionEvent(from, to, TransitionAction.ENTER));
		public void RecordTransitionShouldTransition(TStateId from, TStateId to)
			=> recordedEvents.Enqueue(new TransitionEvent(from, to, TransitionAction.SHOULD_TRANSITION));

		public void RecordCustom(string message)
			=> recordedEvents.Enqueue(new CustomEvent(message));

		public StateBase<TStateId> Track(StateBase<TStateId> state)
		{
			return tracker.Decorate(state);
		}

		private string CreateTraceback()
		{
			StringBuilder builder = new StringBuilder();

			builder.AppendLine();
			foreach (var ev in recordedEvents)
			{
				builder.AppendLine(ev.ToString());
			}

			return builder.ToString();
		}

		public void DiscardAll()
		{
			recordedEvents.Clear();
		}
	}

	public class Recorder : Recorder<string>
	{
	}
}
