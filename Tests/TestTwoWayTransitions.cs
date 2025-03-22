using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestTwoWayTransitions
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
		public void Test_two_way_transitions_work_both_ways()
		{
			bool shouldBeInB = false;

			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTwoWayTransition("A", "B", t => shouldBeInB);

			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);

			shouldBeInB = true;
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);

			shouldBeInB = false;
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);
		}
	}
}
