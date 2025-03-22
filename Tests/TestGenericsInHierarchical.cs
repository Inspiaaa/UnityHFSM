using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestGenericsInHierarchical
	{
		private enum States
		{
			INT_FSM, STRING_FSM
		}

		[Test]
		public void Test_using_different_type_in_nested_fsm_works()
		{
			var fsm = new StateMachine<States>();
			var intFsm = new StateMachine<States, int, string>(needsExitTime: false);
			var stringFsm = new StateMachine<States, string, string>();

			fsm.AddState(States.INT_FSM, intFsm);
			fsm.AddState(States.STRING_FSM, stringFsm);

			intFsm.AddState(1);
			intFsm.AddState(2);
			intFsm.AddTransition(1, 2);

			stringFsm.AddState("A");
			stringFsm.AddState("B");
			stringFsm.AddTransition("A", "B");

			fsm.Init();

			Assert.AreEqual(States.INT_FSM, fsm.ActiveStateName);
			Assert.AreEqual(1, intFsm.ActiveStateName);

			fsm.OnLogic();
			Assert.AreEqual(2, intFsm.ActiveStateName);

			fsm.RequestStateChange(States.STRING_FSM);
			Assert.AreEqual(States.STRING_FSM, fsm.ActiveStateName);
			Assert.AreEqual("A", stringFsm.ActiveStateName);

			fsm.OnLogic();
			Assert.AreEqual("B", stringFsm.ActiveStateName);
		}
	}
}
