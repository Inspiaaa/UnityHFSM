using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestTransitionCallbacks
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
		public void Test_transition_callbacks_work_in_flat_fsm_with_instant_transition()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);

			fsm.AddTransition("A", "B",
				onTransition: t => recorder.RecordCustom("CallbackBefore"),
				afterTransition: t => recorder.RecordCustom("CallbackAfter")
			);

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Custom("CallbackBefore")
				.Exit("A")
				.Enter("B")
				.Custom("CallbackAfter")
				.Logic("B")
				.All();
		}

		[Test]
		public void Test_transition_callbacks_work_in_flat_fsm_with_delayed_transition()
		{
			fsm.AddState("A", recorder.Track(new State(needsExitTime: true)));
			fsm.AddState("B", recorder.TrackedState);

			fsm.AddTransition("A", "B",
				onTransition: t => recorder.RecordCustom("CallbackBefore"),
				afterTransition: t => recorder.RecordCustom("CallbackAfter")
			);

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect.Logic("A").All();

			fsm.StateCanExit();
			recorder.Expect
				.Custom("CallbackBefore")
				.Exit("A")
				.Enter("B")
				.Custom("CallbackAfter")
				.All();
		}

		[Test]
		public void Test_transition_callbacks_work_in_flat_fsm_with_ghost_state()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.Track(new State(isGhostState: true)));
			fsm.AddState("C", recorder.TrackedState);

			fsm.AddTransition("A", "B",
				onTransition: t => recorder.RecordCustom("A->B Before"),
				afterTransition: t => recorder.RecordCustom("A->B After")
			);
			fsm.AddTransition("B", "C",
				onTransition: t => recorder.RecordCustom("B->C Before"),
				afterTransition: t => recorder.RecordCustom("B->C After")
			);

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Custom("A->B Before")
				.Exit("A")
				.Enter("B")
				.Custom("A->B After")
				.Custom("B->C Before")
				.Exit("B")
				.Enter("C")
				.Custom("B->C After")
				.Logic("C")
				.All();
		}

		[Test]
		public void Test_transition_callbacks_work_in_nested_fsm_with_exit_transition()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("Nested", recorder.TrackedState);

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			nested.AddExitTransition("Nested",
				onTransition: t => recorder.RecordCustom("CallbackBefore"),
				afterTransition: t => recorder.RecordCustom("CallbackAfter")
			);

			fsm.Init();
			recorder.Expect
				.Enter("A")
				.Enter("Nested")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Custom("CallbackBefore")
				.Exit("A")
				.Exit("Nested")
				.Enter("B")
				.Custom("CallbackAfter")
				.All();
		}
	}
}
