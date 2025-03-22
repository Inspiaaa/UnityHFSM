using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestHierarchyPath
	{
		[Test]
		public void Test_string_is_correct_for_nested_fsm_using_string_ids()
		{
			var fsm = new StateMachine();
			var move = new StateMachine();
			var jump = new StateMachine();

			fsm.AddState("Move", move);
			move.AddState("Jump", jump);
			jump.AddState("Falling");

			fsm.Init();

			Assert.AreEqual("/Move/Jump/Falling", fsm.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_string_is_empty_when_state_machine_is_not_active()
		{
			var fsm = new StateMachine();
			fsm.AddState("A");
			Assert.AreEqual("", fsm.GetActiveHierarchyPath());
		}

		[Test]
		public void Test_string_is_correct_when_using_mixed_types_for_ids()
		{
			var root = new StateMachine<string, string, string>();
			var a = new StateMachine<string, int, string>();
			var b = new StateMachine<int, bool, string>();

			root.AddState("A", a);
			a.AddState(5, b);
			b.AddState(false);

			root.Init();

			Assert.AreEqual("/A/5/False", root.GetActiveHierarchyPath());
		}
	}
}
