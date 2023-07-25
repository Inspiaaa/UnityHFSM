using NUnit.Framework;
using FSM;
using System;

namespace FSM.Tests
{
	public class TestCanExit
	{
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			fsm = new StateMachine();
		}

		// TODO: canExit instantly on transition
		// TODO: canExit later

		[Test]
		public void Test_fsm_waits_on_state_with_needsExitTime_when_canExit_false()
		{
			fsm.AddState("A", needsExitTime: true, canExit: state => false);
			fsm.AddState("B");

			fsm.AddTransition("A", "B");

			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);
		}

		[Test]
		public void Test_state_with_needsExitTime_can_exit_instantly_when_canExit_true()
		{
			fsm.AddState("A", needsExitTime: true, canExit: state => true);
			fsm.AddState("B");

			fsm.AddTransition("A", "B");

			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_state_with_needsExitTime_can_exit_later_when_canExit_switches_to_true()
		{
			var canExit = false;
			fsm.AddState("A", needsExitTime: true, canExit: state => canExit);
			fsm.AddState("B");

			fsm.AddTransition("A", "B");

			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);

			canExit = true;
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}
	}
}