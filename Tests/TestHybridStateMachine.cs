using NUnit.Framework;

namespace UnityHFSM.Tests
{
	public class TestHybridStateMachine
	{
		private Recorder recorder;
		private StateMachine fsm;

		[SetUp]
		public void Setup()
		{
			recorder = new Recorder();
			fsm = new StateMachine();
		}

		[Test]
		public void Test_hybrid_does_not_throw_exceptions_when_no_callbacks_given()
		{
			var hybrid = new HybridStateMachine();
			fsm.AddState("Main", recorder.Track(hybrid));
			fsm.AddState("Other", recorder.TrackedState);
			hybrid.AddState("A", recorder.TrackedState);

			Assert.DoesNotThrow(() => fsm.Init());
			Assert.DoesNotThrow(() => fsm.OnLogic());
			Assert.DoesNotThrow(() => fsm.RequestStateChange("Other"));
		}

		[Test]
		public void Test_hybrid_behaves_like_normal_fsm()
		{
			var hybrid = new HybridStateMachine();
			fsm.AddState("Main", recorder.Track(hybrid));
			fsm.AddState("Other", recorder.TrackedState);
			hybrid.AddState("A", recorder.TrackedState);

			fsm.Init();
			recorder.Expect
				.Enter("Main")
				.Enter("A")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("Main")
				.Logic("A")
				.All();

			fsm.RequestStateChange("Other");
			recorder.Expect
				.Exit("Main")
				.Exit("A")
				.Enter("Other")
				.All();
		}

		[Test]
		public void Test_order_of_callbacks_is_correct()
		{
			var hybrid = new HybridStateMachine(
				beforeOnEnter: fsm => recorder.RecordCustom("BeforeEnter"),
				afterOnEnter: fsm => recorder.RecordCustom("AfterEnter"),
				beforeOnLogic: fsm => recorder.RecordCustom("BeforeLogic"),
				afterOnLogic: fsm => recorder.RecordCustom("AfterLogic"),
				beforeOnExit: fsm => recorder.RecordCustom("BeforeExit"),
				afterOnExit: fsm => recorder.RecordCustom("AfterExit")
			);
			hybrid.AddState("A", recorder.TrackedState);

			hybrid.OnEnter();
			recorder.Expect
				.Custom("BeforeEnter")
				.Enter("A")
				.Custom("AfterEnter")
				.All();

			hybrid.OnLogic();
			recorder.Expect
				.Custom("BeforeLogic")
				.Logic("A")
				.Custom("AfterLogic")
				.All();

			hybrid.OnExit();
			recorder.Expect
				.Custom("BeforeExit")
				.Exit("A")
				.Custom("AfterExit")
				.All();
		}

		[Test]
		public void Test_hybrid_actions_are_called_after_sub_state_actions()
		{
			var hybrid = new HybridStateMachine();

			hybrid.AddState("A", new State()
				.AddAction("Normal", () => recorder.RecordCustom("A.Normal()"))
				.AddAction<int>("Parameter", value => recorder.RecordCustom($"A.Parameter({value})"))
			);

			hybrid.AddAction("Normal", () => recorder.RecordCustom("Hybrid.Normal()"));
			hybrid.AddAction<int>("Parameter", value => recorder.RecordCustom($"Hybrid.Parameter({value})"));

			hybrid.Init();

			hybrid.OnAction("Normal");
			recorder.Expect
				.Custom("Hybrid.Normal()")
				.Custom("A.Normal()")
				.All();

			hybrid.OnAction<int>("Parameter", 10);
			recorder.Expect
				.Custom("Hybrid.Parameter(10)")
				.Custom("A.Parameter(10)")
				.All();
		}
	}
}
