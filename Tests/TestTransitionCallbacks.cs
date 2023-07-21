using NUnit.Framework;
using FSM;
using System;

namespace FSM.Tests
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

		// TODO: Pending transition
		// TODO: With ghost state

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