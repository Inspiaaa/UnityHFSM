using NUnit.Framework;
using System;

namespace UnityHFSM.Tests
{
	public class TestTransitionAfter
	{
		private (TransitionAfter transition, TestTimer timer)
				CreateTransitionAfterWithDelay(float delay, Func<TransitionAfter<string>, bool> condition = null)
		{
			TransitionAfter transition = new TransitionAfter("A", "B", delay: delay, condition: condition);
			transition.OnEnter();
			TestTimer timer = new TestTimer();
			transition.timer = timer;
			return (transition, timer);
		}

		[Test]
		public void Test_ShouldTransition_is_false_when_not_elapsed()
		{
			var (transition, timer) = CreateTransitionAfterWithDelay(2);
			timer.Elapsed = 0;
			Assert.IsFalse(transition.ShouldTransition());
		}

		[Test]
		public void Test_ShouldTransition_is_true_when_elapsed()
		{
			var (transition, timer) = CreateTransitionAfterWithDelay(1);
			timer.Elapsed = 3;
			Assert.IsTrue(transition.ShouldTransition());
		}

		[Test]
		public void Test_ShouldTransition_is_false_when_elapsed_but_condition_is_false()
		{
			var (transition, timer) = CreateTransitionAfterWithDelay(1, t => false);
			timer.Elapsed = 3;
			Assert.IsFalse(transition.ShouldTransition());
		}

		[Test]
		public void Test_ShouldTransition_is_true_when_elapsed_and_condition_is_true()
		{
			var (transition, timer) = CreateTransitionAfterWithDelay(1, t => true);
			timer.Elapsed = 3;
			Assert.IsTrue(transition.ShouldTransition());
		}
	}
}
