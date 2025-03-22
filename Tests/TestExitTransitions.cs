using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestExitTransitions
	{
		private Recorder recorder;
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			recorder = new Recorder();
			fsm = new StateMachine();
		}

		// TODO: Test exit transitions coexisting beside normal transitions (priorities)

		[Test]
		public void Test_nested_fsm_with_needsExitTime_does_not_exit_on_parent_transition()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("N");

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.SetStartState("A");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.All();
		}

		[Test]
		public void Test_nested_fsm_with_exit_transition_does_not_transition_without_pending_transition()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("A");
			nested.AddExitTransition(new TransitionBase<string>("A", null));

			fsm.AddState("A", nested);
			fsm.AddState("B");

			fsm.SetStartState("A");

			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);
		}

		[Test]
		public void Test_nested_fsm_can_exit_on_pending_transition_with_exit_transition()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("A.X", recorder.TrackedState);
			nested.AddExitTransition(new TransitionBase<string>("A.X", null));

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.SetStartState("A");

			fsm.Init();
			recorder.Expect
				.Enter("A")
				.Enter("A.X")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Exit("A")
				.Exit("A.X")
				.Enter("B")
				.All();
		}

		[Test]
		public void Test_exit_transition_from_ghost_state_works()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("A.X", recorder.TrackedState);
			nested.AddState("A.Y", recorder.Track(new State(isGhostState: true)));
			nested.AddTransition("A.X", "A.Y");
			nested.AddExitTransition(from: "A.Y");

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.SetStartState("A");

			fsm.Init();
			recorder.Expect
				.Enter("A")
				.Enter("A.X")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				// Normal transition from A.X to A.Y
				.Exit("A.X")
				.Enter("A.Y")
				// A.Y is a ghost state => Tries to exit instantly => Triggers an exit transition
				.Exit("A")
				.Exit("A.Y")
				.Enter("B")
				.All();
		}

		[Test]
		public void Test_deeply_nested_exit_transitions_can_lead_to_transition_in_root()
		{
			var nestedA = new StateMachine(needsExitTime: true);
			var nestedB = new StateMachine(needsExitTime: true);
			var nestedC = new StateMachine(needsExitTime: true);

			fsm.AddState("A", recorder.Track(nestedA));
			nestedA.AddState("A.B", recorder.Track(nestedB));
			nestedB.AddState("A.B.C", recorder.Track(nestedC));
			nestedC.AddState("A.B.C.D", recorder.TrackedState);

			fsm.AddState("Z");
			fsm.AddTransition("A", "Z");

			nestedA.AddExitTransition("A.B");
			nestedB.AddExitTransition("A.B.C");
			nestedC.AddExitTransition("A.B.C.D");

			fsm.Init();
			recorder.Expect
				.Enter("A")
				.Enter("A.B")
				.Enter("A.B.C")
				.Enter("A.B.C.D")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Logic("A.B")
				.Logic("A.B.C")
				.Exit("A")
				.Exit("A.B")
				.Exit("A.B.C")
				.Exit("A.B.C.D")
				.All();
		}

		[Test]
		public void Test_exit_transition_succeeds_when_state_with_needsExitTime_can_exit()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("A.X", recorder.Track(new State(needsExitTime: true)));
			nested.AddExitTransition("A.X");

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Logic("A.X")
				.All();

			nested.StateCanExit();

			recorder.Expect
				.Exit("A")
				.Exit("A.X")
				.Enter("B")
				.All();
		}

		[Test]
		public void Test_exit_trigger_transition()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("A.X", recorder.TrackedState);
			nested.AddExitTriggerTransition("Event", "A.X");

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Logic("A.X")
				.All();

			fsm.Trigger("Event");
			recorder.Expect
				.Exit("A")
				.Exit("A.X")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Expect.Logic("B").All();
		}

		[Test]
		public void Test_exit_transition_from_any()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("A.X", recorder.TrackedState);
			nested.AddExitTransitionFromAny();

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Exit("A")
				.Exit("A.X")
				.Enter("B")
				.All();
		}

		[Test]
		public void Test_exit_trigger_transition_from_any()
		{
			var nested = new StateMachine(needsExitTime: true);
			nested.AddState("A.X", recorder.TrackedState);
			nested.AddExitTriggerTransitionFromAny("Event");

			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			fsm.OnLogic();
			recorder.DiscardAll();

			fsm.Trigger("Event");
			recorder.Expect
				.Exit("A")
				.Exit("A.X")
				.Enter("B")
				.All();
		}
	}
}
