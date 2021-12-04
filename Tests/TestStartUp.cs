using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using FSM;

namespace FSM.Tests
{
    public class TestStartUp
    {
        private Recorder recorder;
        private StateMachine fsm;

        [SetUp]
        public void Setup() {
            recorder = new Recorder();
            fsm = new StateMachine();
        }

        [Test]
        public void TestStartupEvents() {
            fsm.AddState("A", recorder.Track(new State()));
            recorder.Check.Empty();

            fsm.Init();
            recorder.Check.Enter("A").All();
            Assert.AreEqual(fsm.ActiveStateName, "A");

            fsm.OnLogic();
            recorder.Check.Logic("A").All();
        }

        [Test]
        public void TestImplicitStartState() {
            fsm.AddState("A");
            fsm.AddState("B");
            fsm.Init();
            Assert.AreEqual(fsm.ActiveStateName, "A");
        }

        [Test]
        public void TestExplicitStartState() {
            fsm.AddState("A");
            fsm.AddState("B");
            fsm.SetStartState("B");
            fsm.Init();
            Assert.AreEqual(fsm.ActiveStateName, "B");
        }

        [Test]
        public void TestExplicitStartStateBeforeAdding() {
            fsm.SetStartState("B");
            fsm.AddState("A");
            fsm.AddState("B");
            fsm.Init();
            Assert.AreEqual(fsm.ActiveStateName, "B");
        }

        [Test]
        public void TestSetStartStateWhileRunning() {
            fsm.AddState("A");
            fsm.AddState("B");
            fsm.Init();

            fsm.OnLogic();
            fsm.SetStartState("B");
            fsm.OnLogic();
            Assert.AreEqual(fsm.ActiveStateName, "A");
        }

        [Test]
        public void TestNotInitializedException() {
            fsm.AddState("A");
            StateBase<string> activeState;
            Assert.Throws<FSM.Exceptions.StateMachineNotInitializedException>(() => activeState = fsm.ActiveState);
        }
    }
}
