using NUnit.Framework;
using System;

namespace UnityHFSM.Tests
{
	public class TestTransitionAfterDynamic
	{
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			fsm = new StateMachine();
			fsm.AddState("A");
			fsm.AddState("B");
			fsm.SetStartState("A");
		}

		private TestTimer CreateTransitionWithDelay(
			Func<TransitionAfterDynamic<string>, float> delay,
			Func<TransitionAfterDynamic<string>, bool> condition = null,
			bool onlyEvaluateDelayOnEnter = false)
		{
			var transition = new TransitionAfterDynamic(
				"A",
				"B",
				delay: delay,
				condition: condition,
				onlyEvaluateDelayOnEnter: onlyEvaluateDelayOnEnter
			);

			var timer = new TestTimer();
			transition.timer = timer;

			fsm.AddTransition(transition);

			return timer;
		}

		/// <summary>
		/// Creates a delay function that uses the passed delays (delays parameter).
		/// On the first call it uses the first delay, on the second call the second delay, and so forth.
		/// If no delays remain, it uses the last delay.
		/// </summary>
		private Func<TransitionAfterDynamic<string>, float> CreateDelays(params float[] delays)
		{
			int index = 0;

			return t => {
				if (index >= delays.Length)
					return delays[delays.Length - 1];

				return delays[index++];
			};
		}

		[Test]
		public void Test_internal_test_util_CreateDelays_works()
		{
			var delays = CreateDelays(1, 2, 3, 4);

			Assert.AreEqual(1, delays(null));
			Assert.AreEqual(2, delays(null));
			Assert.AreEqual(3, delays(null));
			Assert.AreEqual(4, delays(null));
			Assert.AreEqual(4, delays(null));
		}

		[Test]
		public void Test_uses_first_delay_when_onlyEvaluateDelayOnEnter_is_true()
		{
			var timer = CreateTransitionWithDelay(CreateDelays(1, 100), onlyEvaluateDelayOnEnter: true);

			fsm.Init();
			timer.Elapsed = 0;

			fsm.OnLogic();
			fsm.OnLogic();

			Assert.AreEqual("A", fsm.ActiveStateName);

			timer.Elapsed = 2;
			fsm.OnLogic();

			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_uses_later_delays_when_onlyEvaluateDelayOnEnter_is_false()
		{
			var timer = CreateTransitionWithDelay(CreateDelays(100, 100, 100, 1), onlyEvaluateDelayOnEnter: false);

			fsm.Init();
			timer.Elapsed = 2;

			fsm.OnLogic();  // delay = 100
			fsm.OnLogic();  // delay = 100
			fsm.OnLogic();  // delay = 100

			Assert.AreEqual("A", fsm.ActiveStateName);

			fsm.OnLogic();  // delay = 1  < timer.Elapsed

			Assert.AreEqual("B", fsm.ActiveStateName);
		}

		[Test]
		public void Test_respects_condition()
		{
			var shouldTransition = false;

			var timer = CreateTransitionWithDelay(
				CreateDelays(100, 1),
				condition: t => shouldTransition,
				onlyEvaluateDelayOnEnter: false);

			fsm.Init();
			timer.Elapsed = 2;

			fsm.OnLogic();  // delay = 100
			fsm.OnLogic();  // delay = 1
			Assert.AreEqual("A", fsm.ActiveStateName);

			shouldTransition = true;
			fsm.OnLogic();
			Assert.AreEqual("B", fsm.ActiveStateName);
		}
	}
}
