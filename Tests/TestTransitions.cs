using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using FSM;

namespace FSM.Tests
{
	public class TestTransitions
	{
		private Recorder recorder;
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			recorder = new Recorder();
			fsm = new StateMachine();
		}

		/*
		# Terminology:
		- Direct transition: fsm.AddTransition(...)
		- Global transition: fsm.AddTransitionFromAny(...)
		- Trigger transition: fsm.AddTriggerTransition(...)
		- Request transition: fsm.RequestStateChange(...)
		- Force transition: forceInstantly: true

		With exit time:
		- Unwilling: State won't let the fsm exit (yet)
		- Willing: State will instantly let the fsm exit when a transition occurs
		*/

		[Test]
		public void TestDirectTransition()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			fsm.OnLogic();
			recorder.Check
				.Enter("A")
				.Exit("A")
				.Enter("B")
				.Logic("B")
				.All();
		}

		[Test]
		public void TestDirectTransitionWithCondition()
		{
			bool condition = false;

			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B", t => condition);

			fsm.Init();
			fsm.OnLogic();
			recorder.DiscardAll();

			condition = true;
			fsm.OnLogic();
			recorder.Check
				.Exit("A")
				.Enter("B")
				.Logic("B")
				.All();
		}

		[Test]
		public void TestTriggerTransition()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTriggerTransition("Trigger", "A", "B");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Check.Logic("A").All();

			fsm.Trigger("Trigger");
			recorder.Check
				.Exit("A")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Check.Logic("B").All();
		}

		[Test]
		public void TestTriggerTransitionInNestedState()
		{
			var nested = new StateMachine();
			fsm.AddState("Nested", nested);
			nested.AddState("A");
			nested.AddState("B");
			nested.AddTriggerTransition("Trigger", "A", "B");

			fsm.Init();
			Assert.AreEqual(nested.ActiveStateName, "A");

			fsm.Trigger("Trigger");
			Assert.AreEqual(nested.ActiveStateName, "B");
		}

		[Test]
		public void TestRequestTransition()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.Init();
			recorder.DiscardAll();

			fsm.RequestStateChange("B");
			recorder.Check
				.Exit("A")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Check.Logic("B").All();
		}

		[Test]
		public void TestTransitionUnwillingWithExitTime()
		{
			// State A needs exit time and will not let the fsm transition until a condition is met.
			bool condition = false;
			fsm.AddState("A", recorder.Track(new State(
				needsExitTime: true,
				canExit: state => false,
				onLogic: state =>
				{
					if (condition)
					{
						state.fsm.StateCanExit();
					}
				}
			)));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Check
				.Logic("A")
				.All();

			condition = true;
			fsm.OnLogic();
			recorder.Check
				.Logic("A")
				.Exit("A")
				.Enter("B")
				.All();
		}

		[Test]
		public void TestTransitionWillingWithExitTime()
		{
			// State A needs exit time but will instantly let the fsm transition.
			fsm.AddState("A", recorder.Track(new State(needsExitTime: true, canExit: state => true)));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Check
				.Exit("A")
				.Enter("B")
				.Logic("B")
				.All();
		}

		[Test]
		public void TestForceTransitionUnwillingWithExitTime()
		{
			fsm.AddState("A", new State(needsExitTime: true));
			fsm.AddState("B");
			fsm.AddTransition("A", "B", forceInstantly: true);
			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void TestUnwillingPendingState()
		{
			bool condition = false;
			fsm.AddState("A", new State(
				needsExitTime: true,
				onLogic: state =>
				{
					if (condition)
					{
						state.fsm.StateCanExit();
					}
				}
			));
			fsm.AddState("B");
			fsm.AddState("C");
			fsm.Init();

			fsm.RequestStateChange("B");
			fsm.RequestStateChange("C");
			Assert.AreEqual("A", fsm.ActiveStateName);

			condition = true;
			fsm.OnLogic();

			Assert.AreEqual("C", fsm.ActiveStateName);
		}
	}
}
