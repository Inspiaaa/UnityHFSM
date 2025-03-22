using NUnit.Framework;

namespace UnityHFSM.Tests
{
	/*
	Mermaid state diagram for reference:

	```
	stateDiagram-v2
		[*] --> Follow
		Extract: Extract Intel
		Flee: Flee From Player
		Follow: Follow Player

		Flee --> Extract
		Extract --> Flee

		Follow --> Extract
		Extract --> Follow

		state Extract {
			Send: Send Data
			Collect: Collect Data

			[*] --> Collect
			Collect --> Send
			Send --> Collect

			Collect --> [*]
		}
	```
	*/

	public class TestExampleScene
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
		public void Test_example_scene()
		{
			// Advanced test that checks multiple different components working together.
			// This is the hierarchical state machine example from the README
			// that controls the beheviour of an Enemy Spy unit in a space game.

			// ExtractIntel state
			StateMachine extractIntel = new StateMachine(needsExitTime: true);
			extractIntel.AddState("CollectData", recorder.TrackedState);
			extractIntel.AddState("SendData", recorder.TrackedState);

			bool shouldCollectData = true;
			extractIntel.SetStartState("CollectData");
			extractIntel.AddExitTransition("CollectData");
			extractIntel.AddTransition("CollectData", "SendData", t => !shouldCollectData);
			extractIntel.AddTransition("SendData", "CollectData", t => shouldCollectData);

			// States
			fsm.AddState("FleeFromPlayer", recorder.TrackedState);
			fsm.AddState("FollowPlayer", recorder.TrackedState);
			fsm.AddState("ExtractIntel", recorder.Track(extractIntel));

			// Transitions
			bool isInPlayerScanningRange = false;
			bool isPlayerInOwnScanningRange = false;

			fsm.AddTransition(
				"ExtractIntel",
				"FollowPlayer",
				t => !isPlayerInOwnScanningRange
			);

			fsm.AddTransition(
				"FollowPlayer",
				"ExtractIntel",
				t => isPlayerInOwnScanningRange
			);

			fsm.AddTransition(
				"ExtractIntel",
				"FleeFromPlayer",
				t => isInPlayerScanningRange
			);

			fsm.AddTransition(
				"FleeFromPlayer",
				"ExtractIntel",
				t => !isInPlayerScanningRange
			);

			// Start
			fsm.SetStartState("FollowPlayer");
			fsm.Init();
			recorder.Expect.Enter("FollowPlayer").All();

			// Follow the player for one frame
			fsm.OnLogic();
			recorder.Expect.Logic("FollowPlayer").All();

			// Player gets in scanning range => Start collecting data
			isPlayerInOwnScanningRange = true;
			fsm.OnLogic();
			recorder.Expect
				.Exit("FollowPlayer")
				.Enter("ExtractIntel")
				.Enter("CollectData")
				.Logic("ExtractIntel")
				.Logic("CollectData")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("ExtractIntel")
				.Logic("CollectData")
				.All();

			// In the ExtractIntel state: Send the data
			shouldCollectData = false;
			fsm.OnLogic();
			recorder.Expect
				.Logic("ExtractIntel")
				.Exit("CollectData")
				.Enter("SendData")
				.Logic("SendData")
				.All();

			fsm.OnLogic();
			recorder.Expect
				.Logic("ExtractIntel")
				.Logic("SendData")
				.All();

			// In the ExtractIntel state: Collect data
			shouldCollectData = true;
			fsm.OnLogic();
			recorder.Expect
				.Logic("ExtractIntel")
				.Exit("SendData")
				.Enter("CollectData")
				.Logic("CollectData")
				.All();

			// Collect data is interrupted by the player who can now see the Spy Enemy on the edge
			// of the radar.
			isInPlayerScanningRange = true;
			fsm.OnLogic();
			recorder.Expect
				.Logic("ExtractIntel")
				.Exit("ExtractIntel")
				.Exit("CollectData")
				.Enter("FleeFromPlayer")
				.All();

			fsm.OnLogic();
			recorder.Expect.Logic("FleeFromPlayer").All();
		}
	}
}
