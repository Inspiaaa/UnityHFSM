using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestRememberLastState
	{
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			fsm = new StateMachine();
		}

		private void AssertStatesAreActive(string activeHierarchyPath)
		{
			Assert.AreEqual(activeHierarchyPath, fsm.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_nested_fsm_returns_to_start_state_by_default_on_enter()
		{
			var nested = new StateMachine();
			nested.AddState("X");
			nested.AddState("Y");
			nested.SetStartState("Y");

			fsm.AddState("A", nested);
			fsm.AddState("B");

			fsm.Init();
			AssertStatesAreActive("/A/Y");

			nested.RequestStateChange("X");
			AssertStatesAreActive("/A/X");

			fsm.RequestStateChange("B");
			AssertStatesAreActive("/B");

			fsm.RequestStateChange("A");
			AssertStatesAreActive("/A/Y");  // Y is the default start state.
		}

		[Test]
		public void Test_remember_last_state_works()
		{
			var nested = new StateMachine(rememberLastState: true);
			nested.AddState("X");
			nested.AddState("Y");
			nested.AddState("Z");
			nested.SetStartState("X");

			nested.AddTransition("X", "Y");
			nested.AddTransition("Y", "Z");

			fsm.AddState("A", nested);
			fsm.AddState("B");

			fsm.Init();
			AssertStatesAreActive("/A/X");

			fsm.OnLogic();
			fsm.OnLogic();
			AssertStatesAreActive("/A/Z");

			// Here A (the nested fsm) exits. This normally means that when it enters again,
			// it will enter in its original start state (X).
			fsm.RequestStateChange("B");
			AssertStatesAreActive("/B");

			fsm.RequestStateChange("A");
			AssertStatesAreActive("/A/Z");  // Z is the remembered last state.
		}
	}
}
