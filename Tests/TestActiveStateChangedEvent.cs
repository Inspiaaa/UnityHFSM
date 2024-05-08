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
        private List<string> trackedStates;

        [SetUp]
        public void Setup()
        {
            fsm = new StateMachine();
            trackedStates = new List<string>();
        }

        [Test]
        public void Test_active_state_changed_event()
        {
            fsm.StateChanged += state => trackedStates.Add(state != null ? state.name : "null");

            fsm.AddState("A", new State());
            fsm.AddState("B", new State());
            fsm.AddState("C", new State());

            fsm.AddTransition("A", "B");
            fsm.AddTransition("B", "C");

            fsm.SetStartState("A");
            fsm.Init();

            AssertTrackedStated(expected: new[] { "A" });

            fsm.OnLogic();
            AssertTrackedStated(expected: new[] { "A", "B" });

            fsm.OnLogic();
            AssertTrackedStated(expected: new[] { "A", "B", "C" });

            fsm.OnExit();
            AssertTrackedStated(expected: new[] { "A", "B", "C", "null" });
        }

        private void AssertTrackedStated(IEnumerable<string> expected)
        {
            if (!trackedStates.SequenceEqual(expected))
            {
                Assert.Fail($"Tracked active states is not equals with expected. Real: ({string.Join(",", trackedStates)}), Expected : ({string.Join(",", expected)})");
            }
        }
    }
}