using NUnit.Framework;
using FSM;
using System;

namespace FSM.Tests
{
	public class TestHierarchicalTiming
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
        public void Test_parent_fsm_instantly_exits_nested_fsm_if_no_exit_time_is_needed() {
            StateMachine nested = new StateMachine(needsExitTime: false);
            nested.AddState("A.X", recorder.TrackedState);
            fsm.AddState("A", recorder.Track(nested));
            fsm.AddState("B", recorder.TrackedState);

            fsm.SetStartState("A");
            fsm.AddTransition("A", "B");
            fsm.Init();

            recorder.Expect
                .Enter("A")
                .Enter("A.X")
                .All();

            fsm.OnLogic();

            recorder.Expect
                .Exit("A")
                .Exit("A.X")
                .Enter("B")
                .Logic("B")
                .All();
        }

        [Test]
        public void Test_nested_fsm_exits_on_OnLogic_when_in_an_exit_state() {
            StateMachine nested = new StateMachine(needsExitTime: true);
            nested.AddState("A.X", recorder.TrackedState);
            nested.AddState("A.Y", recorder.Track(new State(isExitState: true)));

            fsm.AddState("A", recorder.Track(nested));
            fsm.AddState("B", recorder.TrackedState);

            fsm.SetStartState("A");
            fsm.AddTransition("A", "B");
            fsm.Init();

            recorder.Expect
                .Enter("A")
                .Enter("A.X")
                .All();

            fsm.OnLogic();
            recorder.Expect
                .Logic("A")
                .Logic("A.X")
                .All();

            nested.RequestStateChange("A.Y");

            recorder.Expect
                .Exit("A.X")
                .Enter("A.Y")
                .All();

            fsm.OnLogic();

            recorder.Expect
                .Exit("A")
                .Exit("A.Y")
                .Enter("B")
                .Logic("B")
                .All();
        }

        [Test]
        public void Test_nested_fsm_instantly_exits_when_entering_exit_ghost_state() {
             StateMachine nested = new StateMachine(needsExitTime: true);
            nested.AddState("A.X", recorder.TrackedState);
            nested.AddState("A.Y", recorder.Track(new State(isExitState: true, isGhostState: true)));

            fsm.AddState("A", recorder.Track(nested));
            fsm.AddState("B", recorder.TrackedState);

            fsm.SetStartState("A");
            fsm.AddTransition("A", "B");
            fsm.Init();
            fsm.OnLogic();

            recorder.DiscardAll();

            nested.RequestStateChange("A.Y");

            recorder.Expect
                .Exit("A.X")
                .Enter("A.Y")
                .Exit("A")
                .Exit("A.Y")
                .Enter("B")
                .All();
        }

        [Test]
        public void Test_nested_fsm_can_instantly_exit_if_it_starts_in_exit_state() {
            StateMachine nested = new StateMachine(needsExitTime: true);
            nested.AddState("A.X", new State(isExitState: true));

            fsm.AddState("A", nested);
            fsm.AddState("B");

            fsm.SetStartState("A");
            fsm.AddTransition("A", "B");
            fsm.Init();

            Assert.AreEqual("A", fsm.ActiveStateName);

            fsm.OnLogic();

            Assert.AreEqual("B", fsm.ActiveStateName);
        }
    }
}