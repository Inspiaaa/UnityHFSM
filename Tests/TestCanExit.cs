using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestCanExit
	{
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			fsm = new StateMachine();
		}

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

		[Test]
		public void Test_state_with_needsExitTime_calls_onLogic_before_transitioning_on_delayed_transition()
		{
			var canExit = false;

			var recorder = new Recorder();

			fsm.AddState("A", recorder.Track(new State(
				onLogic: state => recorder.RecordCustom("UserOnLogic"),
				needsExitTime: true,
				canExit: state => canExit)));
			fsm.AddState("B", recorder.TrackedState);

			fsm.Init();
			fsm.OnLogic();

			fsm.RequestStateChange("B");
			Assert.AreEqual("A", fsm.ActiveStateName);

			recorder.DiscardAll();

			canExit = true;
			fsm.OnLogic();

			recorder.Expect
				.Logic("A")
				.Custom("UserOnLogic")
				.Exit("A")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Expect.Logic("B").All();
		}
	}
}
