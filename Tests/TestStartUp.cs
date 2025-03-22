using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestStartUp
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
		public void TestStartupEvents()
		{
			fsm.AddState("A", recorder.Track(new State()));
			recorder.Expect.Empty();

			fsm.Init();
			recorder.Expect.Enter("A").All();
			Assert.AreEqual("A", fsm.ActiveStateName);

			fsm.OnLogic();
			recorder.Expect.Logic("A").All();
		}

		[Test]
		public void Test_fsm_starts_in_implicit_start_state()
		{
			fsm.AddState("A");
			fsm.AddState("B");
			fsm.Init();
			Assert.AreEqual("A", fsm.ActiveStateName);
		}

		[Test]
		public void Test_fsm_starts_in_explicit_start_state()
		{
			fsm.AddState("A");
			fsm.AddState("B");
			fsm.SetStartState("B");
			fsm.Init();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_setting_start_state_before_adding_the_state_works()
		{
			fsm.SetStartState("B");
			fsm.AddState("A");
			fsm.AddState("B");
			fsm.Init();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_setting_start_state_while_running_does_nothing()
		{
			fsm.AddState("A");
			fsm.AddState("B");
			fsm.Init();

			fsm.OnLogic();
			fsm.SetStartState("B");
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);
		}

		[Test]
		public void Test_accessing_active_state_before_init_fails()
		{
			fsm.AddState("A");
			StateBase<string> activeState;
			Assert.Throws<UnityHFSM.Exceptions.StateMachineException>(() => activeState = fsm.ActiveState);
		}

		[Test]
		public void Test_calling_init_before_adding_a_state_fails()
		{
			Assert.Throws<UnityHFSM.Exceptions.StateMachineException>(() => fsm.Init());
		}
	}
}
