using NUnit.Framework;
using FSM;
using System;

namespace FSM.Tests
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

        [Test]
        public void Test_nested_fsm_with_needsExitTime_does_not_exit_on_parent_transition() {
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
        public void Test_nested_fsm_with_exit_transition_does_not_transition_without_pending_transition() {
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
        public void Test_nested_fsm_can_exit_on_pending_transition_with_exit_transition() {
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
        public void Test_exit_transition_from_ghost_state_works() {
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

    }
}