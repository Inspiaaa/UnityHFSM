using NUnit.Framework;
using UnityHFSM.Inspection;

namespace UnityHFSM.Tests
{
	public class TestStateMachinePath
	{
		[Test]
		public void Test_same_paths_are_equal()
		{
			StateMachinePath root = StateMachinePath.Root;

			StateMachinePath pathA1 = new StateMachinePath<string>(root, "A");
			StateMachinePath pathA2 = new StateMachinePath<int>(pathA1, 1);

			StateMachinePath pathB1 = new StateMachinePath<string>(root, "A");
			StateMachinePath pathB2 = new StateMachinePath<int>(pathB1, 1);

			Assert.AreEqual(root, root);
			Assert.AreEqual(pathA1, pathB1);
			Assert.AreEqual(pathA2, pathB2);

			Assert.AreEqual(pathA1.GetHashCode(), pathB1.GetHashCode());
			Assert.AreEqual(pathA2.GetHashCode(), pathB2.GetHashCode());

			Assert.True(pathA1 == pathB1);
			Assert.True(pathA2 == pathB2);
		}

		[Test]
		public void Test_different_paths_are_not_equal()
		{
			StateMachinePath root = StateMachinePath.Root;
			StateMachinePath path1 = new StateMachinePath<string>(root, "A");
			StateMachinePath path2 = new StateMachinePath<int>(path1, 1);

			Assert.AreNotEqual(root, path1);
			Assert.AreNotEqual(path1, path2);
		}

		[Test]
		public void Test_paths_with_same_string_but_different_types_are_not_equal()
		{
			StateMachinePath a1 = new StateMachinePath<int>(0);
			StateMachinePath a2 = new StateMachinePath<int>(a1, 1);
			StateMachinePath a3 = new StateMachinePath<int>(a2, 2);

			StateMachinePath b1 = new StateMachinePath<short>(0);
			StateMachinePath b2 = new StateMachinePath<short>(b1, 1);
			StateMachinePath b3 = new StateMachinePath<short>(b2, 2);

			Assert.AreNotEqual(a3, b3);
			Assert.AreEqual(a3.ToString(), b3.ToString());
		}

		[Test]
		public void Test_IsChildPathOf()
		{
			StateMachinePath root = StateMachinePath.Root;
			StateMachinePath path1 = new StateMachinePath<string>(root, "A");
			StateMachinePath path2 = new StateMachinePath<int>(path1, 1);
			StateMachinePath unrelatedPath = new StateMachinePath<short>(5);

			Assert.False(root.IsChildPathOf(root));

			Assert.True(path1.IsChildPathOf(root));
			Assert.True(path2.IsChildPathOf(root));
			Assert.True(path2.IsChildPathOf(path1));
			Assert.False(path1.IsChildPathOf(path2));

			Assert.False(unrelatedPath.IsChildPathOf(root));
			Assert.False(path2.IsChildPathOf(unrelatedPath));
		}
	}
}
