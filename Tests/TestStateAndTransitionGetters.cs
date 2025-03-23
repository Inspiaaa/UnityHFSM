using NUnit.Framework;
using System.Collections.Generic;
using UnityHFSM.Exceptions;

namespace UnityHFSM.Tests
{
	public class TestStateAndTransitionGetters
	{
		private StateMachine fsm;

		private List<StateBase<string>> states;
		private List<string> stateNames;
		private List<TransitionBase<string>> transitions;
		private List<TransitionBase<string>> transitionsFromAny;
		private Dictionary<string, List<TransitionBase<string>>> triggerTransitionsByEvent;
		private Dictionary<string, List<TransitionBase<string>>> triggerTransitionsFromAnyByEvent;

		[SetUp]
		public void Setup()
		{
			states = new List<StateBase<string>>();
			stateNames = new List<string>();
			transitions = new List<TransitionBase<string>>();
			transitionsFromAny = new List<TransitionBase<string>>();
			triggerTransitionsByEvent = new Dictionary<string, List<TransitionBase<string>>>();
			triggerTransitionsFromAnyByEvent = new Dictionary<string, List<TransitionBase<string>>>();
		}

		private void CreateEmptyStateMachine()
		{
			fsm = new StateMachine();
		}

		private void CreateExampleStateMachine()
		{
			fsm = new StateMachine();

			fsm.SetStartState("A");

			AddState("A");
			AddState("B");
			AddState("C");

			AddTransition("A", "B");
			AddTransition("A", "C");
			AddTransition("C", "A");

			AddTriggerTransition("event1", "A", "B");
			AddTriggerTransition("event1", "A", "C");
			AddTriggerTransition("event2", "A", "C");
			AddTriggerTransition("event1", "C", "A");
			AddTriggerTransition("event2", "B", "C");

			AddTransitionFromAny("A");
			AddTransitionFromAny("A");
			AddTransitionFromAny("B");

			AddTriggerTransitionFromAny("event1", "A");
			AddTriggerTransitionFromAny("event1", "A");
			AddTriggerTransitionFromAny("event2", "A");
			AddTriggerTransitionFromAny("event2", "C");
		}

		[Test]
		public void Test_GetStartStateName()
		{
			CreateExampleStateMachine();
			Assert.AreEqual("A", fsm.GetStartStateName());
		}

		[Test]
		public void Test_GetStartStateName_on_empty_fsm()
		{
			CreateEmptyStateMachine();
			Assert.Throws<StateMachineException>(() => fsm.GetStartStateName());
		}

		[Test]
		public void Test_GetAllStateNames()
		{
			CreateExampleStateMachine();
			Assert.That(fsm.GetAllStateNames(), Is.EquivalentTo(stateNames));
		}

		[Test]
		public void Test_GetAllStateNames_on_empty_fsm()
		{
			CreateEmptyStateMachine();
			Assert.That(fsm.GetAllStateNames(), Is.EquivalentTo(stateNames));
		}

		[Test]
		public void Test_GetAllStates()
		{
			CreateExampleStateMachine();
			Assert.That(fsm.GetAllStates(), Is.EquivalentTo(states));
		}

		[Test]
		public void Test_GetAllStates_on_empty_fsm()
		{
			CreateEmptyStateMachine();
			Assert.That(fsm.GetAllStates(), Is.EquivalentTo(states));
		}

		[Test]
		public void Test_GetAllTransitions()
		{
			CreateExampleStateMachine();
			Assert.That(fsm.GetAllTransitions(), Is.EquivalentTo(transitions));
		}

		[Test]
		public void Test_GetAllTransitions_on_empty_fsm()
		{
			CreateEmptyStateMachine();
			Assert.That(fsm.GetAllTransitions(), Is.EquivalentTo(transitions));
		}

		[Test]
		public void Test_GetAllTransitionsFromAny()
		{
			CreateExampleStateMachine();
			Assert.That(fsm.GetAllTransitionsFromAny(), Is.EquivalentTo(transitionsFromAny));
		}

		[Test]
		public void Test_GetAllTransitionsFromAny_on_empty_fsm()
		{
			CreateEmptyStateMachine();
			Assert.That(fsm.GetAllTransitionsFromAny(), Is.EquivalentTo(transitionsFromAny));
		}

		[Test]
		public void Test_GetAllTriggerTransitions()
		{
			CreateExampleStateMachine();
			Assert.That(fsm.GetAllTriggerTransitions(), Is.EquivalentTo(triggerTransitionsByEvent));
		}

		[Test]
		public void Test_GetAllTriggerTransitions_on_empty_fsm()
		{
			CreateEmptyStateMachine();
			Assert.That(fsm.GetAllTriggerTransitions(), Is.EquivalentTo(triggerTransitionsByEvent));
		}

		[Test]
		public void Test_GetAllTriggerTransitionsFromAny()
		{
			CreateExampleStateMachine();
			Assert.That(fsm.GetAllTriggerTransitionsFromAny(), Is.EquivalentTo(triggerTransitionsFromAnyByEvent));
		}

		[Test]
		public void Test_GetAllTriggerTransitionsFromAny_on_empty_fsm()
		{
			CreateEmptyStateMachine();
			Assert.That(fsm.GetAllTriggerTransitionsFromAny(), Is.EquivalentTo(triggerTransitionsFromAnyByEvent));
		}

		private void AddState(string name)
		{
			var state = new State();
			stateNames.Add(name);
			states.Add(state);
			fsm.AddState(name, state);
		}

		private void AddTransition(string from, string to)
		{
			var transition = new Transition(from, to);
			fsm.AddTransition(transition);
			transitions.Add(transition);
		}

		private void AddTriggerTransition(string trigger, string from, string to)
		{
			var transition = new Transition(from, to);
			fsm.AddTriggerTransition(trigger, transition);

			if (!triggerTransitionsByEvent.ContainsKey(trigger))
			{
				triggerTransitionsByEvent.Add(trigger, new List<TransitionBase<string>>());
			}

			triggerTransitionsByEvent[trigger].Add(transition);
		}

		private void AddTransitionFromAny(string to)
		{
			var transition = new Transition(null, to);
			fsm.AddTransitionFromAny(transition);
			transitionsFromAny.Add(transition);
		}

		private void AddTriggerTransitionFromAny(string trigger, string to)
		{
			var transition = new Transition("", to);
			fsm.AddTriggerTransitionFromAny(trigger, transition);

			if (!triggerTransitionsFromAnyByEvent.ContainsKey(trigger))
			{
				triggerTransitionsFromAnyByEvent.Add(trigger, new List<TransitionBase<string>>());
			}

			triggerTransitionsFromAnyByEvent[trigger].Add(transition);
		}
	}
}
