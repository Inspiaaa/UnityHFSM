using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestParallelStates
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
		public void Test_ps_sets_up_implicit_names_for_string_states()
		{
			var stateA = new State();
			var stateB = new State();
			var stateC = new State();

			var ps = new ParallelStates(stateA, stateB, stateC);
			Assert.AreEqual("0", stateA.name);
			Assert.AreEqual("1", stateB.name);
			Assert.AreEqual("2", stateC.name);
		}

		[Test]
		public void Test_ps_calls_OnEnter_OnLogic_OnExit_on_child_states()
		{
			fsm.AddState("Start", new ParallelStates()
				.AddState("A", recorder.TrackedState)
				.AddState("B", recorder.TrackedState)
			);
			fsm.AddState("Other", recorder.TrackedState);

			fsm.Init();
			recorder.Expect
				.Enter("A")
				.Enter("B")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("A")
				.Logic("B")
				.All();

			fsm.RequestStateChange("Other");
			recorder.Expect
				.Exit("A")
				.Exit("B")
				.Enter("Other")
				.All();
		}

		[Test]
		public void Test_ps_exits_instantly_when_needsExitTime_is_false()
		{
			fsm.AddState("A", new ParallelStates(needsExitTime: false, new State(needsExitTime: true)));
			fsm.AddState("B");
			fsm.AddTransition("A", "B");

			fsm.Init();
			Assert.AreEqual("A", fsm.ActiveStateName);
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_ps_exits_instantly_on_request_when_any_child_state_can_exit()
		{
			bool firstCanExit = false;
			bool secondCanExit = false;

			fsm.AddState("A", new ParallelStates(needsExitTime: true,
				new State(needsExitTime: true, canExit: state => firstCanExit),
				new State(needsExitTime: true, canExit: state => secondCanExit)
			));

			fsm.AddState("B");

			fsm.Init();
			Assert.AreEqual("A", fsm.ActiveStateName);

			firstCanExit = true;
			fsm.RequestStateChange("B");
			Assert.AreEqual("B", fsm.ActiveStateName);

			firstCanExit = false;
			fsm.RequestStateChange("A");
			Assert.AreEqual("A", fsm.ActiveStateName);

			secondCanExit = true;
			fsm.RequestStateChange("B");
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_ps_exits_later_when_any_child_state_can_exit()
		{
			bool firstCanExit = false;
			bool secondCanExit = false;

			fsm.AddState("A", new ParallelStates(needsExitTime: true,
				new State(needsExitTime: true, canExit: state => firstCanExit),
				new State(needsExitTime: true, canExit: state => secondCanExit)
			));

			fsm.AddState("B");

			fsm.Init();
			fsm.RequestStateChange("B");
			fsm.OnLogic();
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);

			firstCanExit = true;
			fsm.OnLogic();
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);

			firstCanExit = false;
			fsm.RequestStateChange("A");
			Assert.AreEqual("A", fsm.ActiveStateName);

			fsm.RequestStateChange("B");
			secondCanExit = true;
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_ps_exits_on_exit_transition_of_child_fsm()
		{
			var nestedFsm = new StateMachine(needsExitTime: true);
			nestedFsm.AddState("Start", needsExitTime: true);
			nestedFsm.AddExitTransition("Start");

			fsm.AddState("A", new ParallelStates(
				needsExitTime: true,
				new State(needsExitTime: true),
				nestedFsm
			));
			fsm.AddState("B");
			fsm.AddTransition("A", "B");

			fsm.Init();
			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);

			nestedFsm.StateCanExit();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_ps_with_exit_time_exits_instantly_when_canExit_returns_true()
		{
			fsm.AddState("A", new ParallelStates(
				canExit: state => true,
				needsExitTime: true,
				new State(needsExitTime: true)));
			fsm.AddState("B");

			fsm.Init();
			Assert.AreEqual("A", fsm.ActiveStateName);
			fsm.RequestStateChange("B");
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_ps_with_exit_time_exits_as_soon_as_canExit_returns_true()
		{
			bool canExit = false;
			fsm.AddState("A", new ParallelStates(
				canExit: state => canExit,
				needsExitTime: true,
				new State(needsExitTime: true)));
			fsm.AddState("B");

			fsm.Init();

			fsm.RequestStateChange("B");

			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);

			canExit = true;
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_ps_ignores_child_StateCanExit_calls_when_canExit_is_defined()
		{
			bool canExit = false;
			fsm.AddState("A", new ParallelStates(
				canExit: state => canExit,
				needsExitTime: true,
				new State(canExit: s => true, needsExitTime: true)));

			fsm.AddTransition("A", "B");

			fsm.AddState("B");

			fsm.Init();

			fsm.RequestStateChange("B");
			Assert.AreEqual("A", fsm.ActiveStateName);

			fsm.OnLogic();
			Assert.AreEqual("A", fsm.ActiveStateName);

			canExit = true;
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_ps_active_hierarchy_path_for_named_states()
		{
			var nestedFsm = new StateMachine();
			nestedFsm.AddState("D");
			fsm.AddState("PS", new ParallelStates()
				.AddState("A", new State())
				.AddState("B", new State())
				.AddState("C", nestedFsm)
			);

			fsm.Init();
			Assert.AreEqual("/PS/(A & B & C/D)", fsm.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_ps_active_hierarchy_path_for_single_child_state()
		{
			fsm.AddState("PS", new ParallelStates()
				.AddState("A", new State())
			);

			fsm.Init();
			Assert.AreEqual("/PS/A", fsm.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_ps_active_hierarchy_path_for_no_child_states()
		{
			fsm.AddState("PS", new ParallelStates());

			fsm.Init();
			Assert.AreEqual("/PS", fsm.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_ps_active_hierarchy_path_for_nameless_states()
		{
			fsm.AddState("PS", new ParallelStates<string, object, string>(
				new State<object>(), new State<object>()
			));

			fsm.Init();
			Assert.AreEqual("/PS", fsm.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_ps_active_hierarchy_path_does_not_throw_error_when_root()
		{
			var root = new ParallelStates()
				.AddState("A", new State())
				.AddState("B", new State());

			Assert.DoesNotThrow(() => root.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_ps_child_state_can_use_different_type_for_id()
		{
			var ps = new ParallelStates<string, int, string>();
			ps.AddState(0, new State<int>());
			ps.AddState(1, new State<int>());

			fsm.AddState("A", ps);
			fsm.Init();
			fsm.OnLogic();
		}

		[Test]
		public void Test_ps_reacts_to_global_trigger()
		{
			var a = new StateMachine();
			a.AddState("A");
			a.AddState("B");
			a.AddTriggerTransition("T", "A", "B");

			var b = new StateMachine();
			b.AddState("C");
			b.AddState("D");
			b.AddTriggerTransition("T", "C", "D");

			fsm.AddState("root", new ParallelStates(a, b));
			fsm.Init();

			fsm.Trigger("T");
			Assert.AreEqual("B", a.ActiveStateName);
			Assert.AreEqual("D", b.ActiveStateName);
		}

		[Test]
		public void Test_ps_does_not_call_OnLogic_on_second_state_after_exit()
		{
			var fsm = new StateMachine();

			bool wasOnLogicCalledOn1 = false;
			bool wasOnLogicCalledOn2 = false;

			var ps = new ParallelStates(needsExitTime: true)
				.AddState("1", new State(
					onLogic: state => {
						wasOnLogicCalledOn1 = true;
						state.fsm.StateCanExit();
					}
				))
				.AddState("2", new State(
					onLogic: state => wasOnLogicCalledOn2 = true
				));

			fsm.AddState("A", ps);
			fsm.AddState("B");

			fsm.Init();
			fsm.RequestStateChange("B");

			Assert.AreEqual("A", fsm.ActiveStateName);

			fsm.OnLogic();

			Assert.AreEqual("B", fsm.ActiveStateName);
			Assert.IsTrue(wasOnLogicCalledOn1);
			Assert.IsFalse(wasOnLogicCalledOn2);
		}
	}
}