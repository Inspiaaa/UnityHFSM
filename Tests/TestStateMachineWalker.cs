using NUnit.Framework;
using UnityHFSM.Inspection;

namespace UnityHFSM.Tests
{
	public class TestStateMachineWalker
	{
		[Test]
		public void Test_GetActiveStatePath()
		{
			var nested2 = new StateMachine<int, bool, string>();
			nested2.AddState(true);

			var nested1 = new StateMachine<string, int, string>();
			nested1.AddState(5, nested2);

			var fsm = new StateMachine();
			fsm.AddState("A", nested1);
			fsm.AddState("B");

			fsm.SetStartState("A");
			fsm.Init();

			StateMachinePath expectedPath = StateMachinePath.Root.Join("A").Join(5).Join(true);
			StateMachinePath path = StateMachineWalker.GetActiveStatePath(fsm);
			Assert.AreEqual(expectedPath, path);

			fsm.RequestStateChange("B");

			expectedPath = StateMachinePath.Root.Join("B");
			path = StateMachineWalker.GetActiveStatePath(fsm);
			Assert.AreEqual(expectedPath, path);
		}

		[Test]
		public void Test_GetPathOfState()
		{
			var nested2 = new StateMachine<int, bool, string>();
			var stateTrue = new State<bool>();
			var stateFalse = new State<bool>();
			nested2.AddState(true, stateTrue);
			nested2.AddState(false, stateFalse);

			var nested1 = new StateMachine<string, int, string>();
			nested1.AddState(5, nested2);

			var fsm = new StateMachine();
			fsm.AddState("A", nested1);
			var stateB = new State();
			fsm.AddState("B", stateB);

			Assert.AreEqual(
				StateMachinePath.Root,
				StateMachineWalker.GetPathOfState(fsm)
			);

			Assert.AreEqual(
				StateMachinePath.Root.Join("A"),
				StateMachineWalker.GetPathOfState(nested1)
			);

			Assert.AreEqual(
				StateMachinePath.Root.Join("B"),
				StateMachineWalker.GetPathOfState(stateB)
			);

			Assert.AreEqual(
				StateMachinePath.Root.Join("A").Join(5),
				StateMachineWalker.GetPathOfState(nested2)
			);

			Assert.AreEqual(
				StateMachinePath.Root.Join("A").Join(5).Join(true),
				StateMachineWalker.GetPathOfState(stateTrue)
			);
		}

		[Test]
		public void Test_GetStringPathOfState()
		{
			var nested2 = new StateMachine<int, bool, string>();
			var stateTrue = new State<bool>();
			var stateFalse = new State<bool>();
			nested2.AddState(true, stateTrue);
			nested2.AddState(false, stateFalse);

			var nested1 = new StateMachine<string, int, string>();
			nested1.AddState(5, nested2);

			var fsm = new StateMachine();
			fsm.AddState("A", nested1);
			var stateB = new State();
			fsm.AddState("B", stateB);

			Assert.AreEqual(
				StateMachineWalker.GetPathOfState(fsm).ToString(),
				StateMachineWalker.GetStringPathOfState(fsm)
			);

			Assert.AreEqual(
				StateMachineWalker.GetPathOfState(nested1).ToString(),
				StateMachineWalker.GetStringPathOfState(nested1)
			);

			Assert.AreEqual(
				StateMachineWalker.GetPathOfState(stateB).ToString(),
				StateMachineWalker.GetStringPathOfState(stateB)
			);

			Assert.AreEqual(
				StateMachineWalker.GetPathOfState(nested2).ToString(),
				StateMachineWalker.GetStringPathOfState(nested2)
			);

			Assert.AreEqual(
				StateMachineWalker.GetPathOfState(stateTrue).ToString(),
				StateMachineWalker.GetStringPathOfState(stateTrue)
			);
		}
	}
}
