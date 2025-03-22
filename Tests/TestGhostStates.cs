using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestGhostStates
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
		public void Test_fsm_performs_one_transition_for_non_ghost_state()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddState("C", recorder.TrackedState);

			fsm.AddTransition("A", "B");
			fsm.AddTransition("B", "C");

			fsm.SetStartState("A");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Exit("A")
				.Enter("B")
				.Logic("B")
				.All();
		}

		[Test]
		public void Test_fsm_quickly_transitions_over_ghost_state_for_on_logic()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.Track(new StateBase(needsExitTime: false, isGhostState: true)));
			fsm.AddState("C", recorder.Track(new StateBase(needsExitTime: false, isGhostState: true)));
			fsm.AddState("D", recorder.TrackedState);

			fsm.AddTransition("A", "B");
			fsm.AddTransition("B", "C");
			fsm.AddTransition("C", "D");

			fsm.SetStartState("A");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Exit("A")
				.Enter("B")
				.Exit("B")
				.Enter("C")
				.Exit("C")
				.Enter("D")
				.Logic("D")
				.All();
		}

		[Test]
		public void Test_fsm_respects_needsExitTime_of_ghost_state()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.Track(
				new State(needsExitTime: true, isGhostState: false)
			));
			fsm.AddState("C", recorder.TrackedState);

			fsm.AddTransition("A", "B");
			fsm.AddTransition("B", "C");

			fsm.SetStartState("A");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Exit("A")
				.Enter("B")
				.Logic("B")
				.All();

			fsm.OnLogic();
			recorder.Expect.Logic("B").All();

			fsm.StateCanExit();
			recorder.Expect
				.Exit("B")
				.Enter("C");
		}
	}
}
