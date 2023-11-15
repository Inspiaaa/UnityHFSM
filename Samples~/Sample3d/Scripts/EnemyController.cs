using System.Collections;
using UnityEngine;
using UnityHFSM; // Import the required classes for the state machine

namespace UnityHFSM.Samples.Sample3d
{
    public class EnemyController : MonoBehaviour
    {
        private StateMachine fsm;
        public float playerScanningRange = 4f;
        public float ownScanningRange = 6f;

        float DistanceToPlayer()
        {
            // This implementation is an example and may differ for your scene setup
            Vector3 player = PlayerController.Instance.transform.position;
            return Vector3.Distance(transform.position, player);
        }

        void MoveTowardsPlayer(float speed)
        {
            // This implementation is an example and may differ for your scene setup
            Vector3 player = PlayerController.Instance.transform.position;
            transform.position = Vector3.MoveTowards(transform.position, player, speed * Time.deltaTime);
        }

        void RotateAtSpeed(float speed)
        {
            transform.eulerAngles += new Vector3(0, 0, speed * Time.deltaTime);
        }

        IEnumerator SendData(CoState<string> state)
        {
            while (state.timer.Elapsed < 2)
            {
                RotateAtSpeed(100f);
                // Wait until the next frame
                yield return null;
            }

            while (state.timer.Elapsed < 4)
            {
                RotateAtSpeed(-100f);
                yield return null;
            }

            state.timer.Reset();
            // Because needsExitTime is true, we have to tell the FSM when it can
            // safely exit the state
            state.fsm.StateCanExit();
        }

        void Start()
        {
            fsm = new StateMachine();

            // This is the nested state machine
            StateMachine extractIntel = new StateMachine(needsExitTime: false);
            fsm.AddState("ExtractIntel", extractIntel);

            extractIntel.AddState("SendData",
                onLogic: (state) =>
                {
                    // When the state has been active for more than 5 seconds,
                    // notify the fsm that the state can cleanly exit
                    if (state.timer.Elapsed > 5)
                        state.fsm.StateCanExit();

                    // Make the enemy turn at 100 degrees per second
                    RotateAtSpeed(100f);
                },
                // This means the state won't instantly exit when a transition should happen
                // but instead the state machine waits until it is given permission to change state
                needsExitTime: true
            );

            // Unity Coroutines
            // extractIntel.AddState("SendData", new CoState(
            //     this
            //     onLogic: SendData,
            //     needsExitTime: true
            // ));

            // Class based architecture
            // extractIntel.AddState("SendData", new CustomSendData(this));

            extractIntel.AddState("CollectData",
                onLogic: (state) =>
                {
                    if (state.timer.Elapsed > 5) state.fsm.StateCanExit();
                },
                needsExitTime: true
            );

            // A transition without a condition
            extractIntel.AddTransition("SendData", "CollectData");
            extractIntel.AddTransition("CollectData", "SendData");
            extractIntel.SetStartState("CollectData");

            fsm.AddState("FollowPlayer",
                onLogic: (state) =>
                {
                    MoveTowardsPlayer(1);
                    if (DistanceToPlayer() < ownScanningRange)
                    {
                        fsm.RequestStateChange("ExtractIntel");
                    }
                }
            );

            fsm.AddState("FleeFromPlayer",
                onLogic: (state) => MoveTowardsPlayer(-1)
            );

            // This configures the entry point of the state machine
            fsm.SetStartState("FollowPlayer");

            fsm.AddTransition(
                "ExtractIntel",
                "FollowPlayer",
                (transition) => DistanceToPlayer() > ownScanningRange);

            fsm.AddTransition(
                "FollowPlayer",
                "ExtractIntel",
                (transition) => DistanceToPlayer() < ownScanningRange);

            fsm.AddTransition(
                "ExtractIntel",
                "FleeFromPlayer",
                (transition) => DistanceToPlayer() < playerScanningRange);

            fsm.AddTransition(
                "FleeFromPlayer",
                "ExtractIntel",
                (transition) => DistanceToPlayer() > playerScanningRange);

            // Initialises the state machine and must be called before OnLogic() is called
            fsm.Init();
        }

        void Update()
        {
            fsm.OnLogic();
        }
    }
}
