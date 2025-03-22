using NUnit.Framework;
using System;

namespace UnityHFSM.Tests
{
	public class TestActions
	{
		private Recorder recorder;
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			recorder = new Recorder();
			fsm = new StateMachine();
		}

		[Test]
		public void Test_calling_existing_action_works()
		{
			bool called = false;
			var state = new ActionState(false).AddAction("Action", () => called = true);

			Assert.IsFalse(called);
			state.OnAction("Action");
			Assert.IsTrue(called);
		}

		[Test]
		public void Test_calling_non_existant_action_does_not_throw_exception_when_no_actions_added()
		{
			var state = new ActionState(false);

			Assert.DoesNotThrow(() => state.OnAction("NonExistantAction"));
			Assert.DoesNotThrow(() => state.OnAction<string>("NonExistantAction", ""));
		}

		[Test]
		public void Test_calling_non_existant_action_does_nothing()
		{
			bool called = false;
			var state = new ActionState(false).AddAction("Action", () => called = true);

			state.OnAction("NonExistantAction");
			Assert.IsFalse(called);
		}

		[Test]
		public void Test_calling_existing_action_with_param_works()
		{
			int value = 0;
			var state = new ActionState(false).AddAction<int>("Action", param => value = param);

			state.OnAction<int>("Action", 5);
			Assert.AreEqual(5, value);
		}

		[Test]
		public void Test_calling_non_existant_action_with_param_does_nothing()
		{
			int value = 0;
			var state = new ActionState(false).AddAction<int>("Action", param => value = param);

			state.OnAction<int>("NonExistantAction", 0);
			Assert.AreEqual(0, value);
		}

		[Test]
		public void Test_calling_action_with_wrong_param_type_fails()
		{
			int value = 0;
			var state = new ActionState(false).AddAction<int>("Action", param => value = param);

			Assert.Throws<InvalidOperationException>(() => state.OnAction<bool>("Action", false));
		}

		[Test]
		public void Test_fsm_propagates_action_to_active_state()
		{
			bool called = false;
			fsm.AddState("A", new State().AddAction("Action", () => called = true));
			fsm.Init();

			Assert.IsFalse(called);
			fsm.OnAction("Action");
			Assert.IsTrue(called);
		}

		[Test]
		public void Test_nested_fsm_propagates_action_to_active_state()
		{
			bool called = false;
			var nested = new StateMachine();
			fsm.AddState("Nested", nested);
			nested.AddState("A", new State().AddAction("Action", () => called = true));
			fsm.Init();

			fsm.OnAction("Action");
			Assert.IsTrue(called);
		}
	}
}
