using NUnit.Framework;

namespace UnityHFSM.Tests
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
		- Direct: fsm.AddTransition(...)
		- Global: fsm.AddTransitionFromAny(...)
		- Trigger: fsm.AddTriggerTransition(...)
		- Request: fsm.RequestStateChange(...)
		- Force: forceInstantly: true
		- Combination of the above, e.g. ForceGlobalTrigger

		With exit time:
		- Unwilling: State won't let the fsm exit (yet)
		- Willing: State will instantly let the fsm exit when a transition occurs

		Nested State machine:
		- WithNestedFsm: The transition applies to a state machine that contains another state machine
		- InNestedFsm: The transition is in the nested state of the parent state machine
		*/

		[Test]
		public void Test_direct_transition()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

			fsm.Init();
			fsm.OnLogic();
			recorder.Expect
				.Enter("A")
				.Exit("A")
				.Enter("B")
				.Logic("B")
				.All();
		}

		[Test]
		public void Test_direct_transition_with_condition()
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
			recorder.Expect
				.Exit("A")
				.Enter("B")
				.Logic("B")
				.All();
		}

		[Test]
		public void Test_request_transition_with_a_nested_fsm()
		{
			var nested = new StateMachine(needsExitTime: false);
			fsm.AddState("A", recorder.Track(nested));
			fsm.AddState("B", recorder.TrackedState);
			nested.AddState("A.X", recorder.TrackedState);

			fsm.Init();
			fsm.OnLogic();
			recorder.Expect
				.Enter("A")
				.Enter("A.X")
				.Logic("A")
				.Logic("A.X")
				.All();

			fsm.RequestStateChange("B");
			recorder.Expect
				.Exit("A")
				.Exit("A.X")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("B")
				.All();
		}

		[Test]
		public void Test_trigger_transition()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTriggerTransition("Trigger", "A", "B");

			fsm.Init();
			recorder.DiscardAll();

			fsm.OnLogic();
			recorder.Expect.Logic("A").All();

			fsm.Trigger("Trigger");
			recorder.Expect
				.Exit("A")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Expect.Logic("B").All();
		}

		[Test]
		public void Test_activating_non_existent_trigger_does_not_fail()
		{
			fsm.AddState("A");
			fsm.Init();
			Assert.DoesNotThrow(() => fsm.Trigger("Trigger"));
		}

		[Test]
		public void Test_activating_trigger_of_root_fsm_leads_to_transition_in_nested_fsm()
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
		public void Test_request_transition()
		{
			fsm.AddState("A", recorder.TrackedState);
			fsm.AddState("B", recorder.TrackedState);
			fsm.Init();
			recorder.DiscardAll();

			fsm.RequestStateChange("B");
			recorder.Expect
				.Exit("A")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Expect.Logic("B").All();
		}

		[Test]
		public void Test_fsm_only_transitions_when_the_active_state_can_exit()
		{
			// State "A" needs exit time and will not let the fsm transition until a condition is met.
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
			recorder.Expect
				.Logic("A")
				.All();

			condition = true;
			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Exit("A")
				.Enter("B")
				.All();
		}

		[Test]
		public void Test_fsm_transitions_instantly_because_the_active_state_with_exit_time_can_exit()
		{
			// State A needs exit time but will instantly let the fsm transition.
			fsm.AddState("A", recorder.Track(new State(needsExitTime: true, canExit: state => true)));
			fsm.AddState("B", recorder.TrackedState);
			fsm.AddTransition("A", "B");

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
		public void Test_forced_direct_transition_overrides_exit_time()
		{
			fsm.AddState("A", new State(needsExitTime: true));
			fsm.AddState("B");
			fsm.AddTransition("A", "B", forceInstantly: true);
			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_fsm_transitions_to_correct_pending_state_once_the_active_state_can_exit()
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
