using NUnit.Framework;
using UnityHFSM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityHFSM.Tests
{
    public class TestActiveStateChangedEvent
    {
        private StateMachine fsm;
        private Recorder recorder;

        [SetUp]
        public void Setup()
        {
            fsm = new StateMachine();
            recorder = new Recorder();
        }

        [Test]
        public void Test_active_state_changed_event()
        {
            fsm.StateChanged += state => recorder.RecordCustom($"StateChanged({state.name})");

            fsm.AddState("A", recorder.TrackedState);
            fsm.AddState("B", recorder.TrackedState);
            fsm.AddState("C", recorder.TrackedState);

            fsm.AddTransition("A", "B");
            fsm.AddTransition("B", "C");

            fsm.SetStartState("A");
            fsm.Init();

            recorder.Expect
                .Enter("A")
                .Custom("StateChanged(A)")
                .All();

            fsm.OnLogic();
            recorder.Expect
                .Exit("A")
                .Enter("B")
                .Custom("StateChanged(B)")
                .Logic("B")
                .All();

            fsm.OnLogic();
            recorder.Expect
                .Exit("B")
                .Enter("C")
                .Custom("StateChanged(C)")
                .Logic("C")
                .All();

            fsm.OnExit();
            recorder.Expect
                .Exit("C")
                .All();
        }
    }
}