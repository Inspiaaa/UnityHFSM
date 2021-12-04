using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using FSM;

namespace FSM.Tests
{
    public class TestStateMachine
    {
        private Recorder recorder;
        private StateMachine fsm;

        [SetUp]
        public void Setup() {
            recorder = new Recorder();
            fsm = new StateMachine();
        }

        [Test]
        public void TestStartup() {
            fsm.AddState("A", recorder.Track(new State()));
            recorder.Check.Empty();

            fsm.Init();
            recorder.Check.Enter("A").All();
            Assert.AreEqual(fsm.ActiveStateName, "A");

            fsm.OnLogic();
            recorder.Check.Logic("A").All();
        }

        [Test]
        public void TestDirectTransitionWithCondition() {
            bool condition = false;

            fsm.AddState("A", recorder.TrackedState);
            fsm.AddState("B", recorder.TrackedState);
            fsm.AddTransition("A", "B", t => condition);

            fsm.Init();
            fsm.OnLogic();
            recorder.DiscardAll();

            condition = true;
            fsm.OnLogic();
            recorder.Check
                .Exit("A")
                .Enter("B")
                .Logic("B")
                .All();
        }

        [Test]
        public void TestTransitionWithExitTime() {
            fsm.AddState("A", recorder.Track(new State(needsExitTime: true, canExit: state => true)));
            fsm.AddState("B", recorder.TrackedState);
            fsm.AddTransition("A", "B");

            fsm.Init();
            fsm.OnLogic();
            recorder.DiscardAll();

            fsm.OnLogic();
            recorder.Check
                .Exit("A")
                .Enter("B")
                .Logic("B")
                .All();
        }
    }
}
