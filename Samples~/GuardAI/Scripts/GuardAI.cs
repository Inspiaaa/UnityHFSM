using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityHFSM;  // Import UnityHFSM

namespace UnityHFSM.Samples.GuardAI {

public class GuardAI : MonoBehaviour
{
    // Declare the finite state machine
    private StateMachine fsm;

    // Parameters (can be changed in the inspector)
    public float searchSpotRange = 10;
    public float attackRange = 3;

    public float searchTime = 20;  // in seconds

    public float patrolSpeed = 2;
    public float chaseSpeed = 4;
    public float attackSpeed = 2;

    public Vector2[] patrolPoints;

    // Internal fields
    private Animator animator;
    private Text stateDisplayText;
    private int patrolDirection = 1;
    private Vector2 lastSeenPlayerPosition;

    // Helper methods (depend on how your scene has been set up)
    private Vector2 playerPosition => PlayerController.Instance.transform.position;
    private float distanceToPlayer => Vector2.Distance(playerPosition, transform.position);

    void Start()
    {
        animator = GetComponent<Animator>();
        stateDisplayText = GetComponentInChildren<Text>();

        fsm = new StateMachine();

        // Fight FSM
        var fightFsm = new HybridStateMachine(
            beforeOnLogic: state => MoveTowards(playerPosition, attackSpeed, minDistance: 1),
            needsExitTime: true
        );

        fightFsm.AddState("Wait", onEnter: state => animator.Play("GuardIdle"));
        fightFsm.AddState("Telegraph", onEnter: state => animator.Play("GuardTelegraph"));
        fightFsm.AddState("Hit",
            onEnter: state => {
                animator.Play("GuardHit");
                // TODO: Cause damage to player if in range.
            }
        );

        // Because the exit transition should have the highest precedence,
        // it is added before the other transitions.
        fightFsm.AddExitTransition("Wait");

        fightFsm.AddTransition(new TransitionAfter("Wait", "Telegraph", 0.5f));
        fightFsm.AddTransition(new TransitionAfter("Telegraph", "Hit", 0.42f));
        fightFsm.AddTransition(new TransitionAfter("Hit", "Wait", 0.5f));

        // Root FSM
        fsm.AddState("Patrol", new CoState(this, Patrol, loop: false));
        fsm.AddState("Chase", new State(
            onLogic: state => MoveTowards(playerPosition, chaseSpeed)
        ));
        fsm.AddState("Fight", fightFsm);
        fsm.AddState("Search", new CoState(this, Search, loop: false));

        fsm.SetStartState("Patrol");

        fsm.AddTriggerTransition("PlayerSpotted", "Patrol", "Chase");
        fsm.AddTwoWayTransition("Chase", "Fight", t => distanceToPlayer <= attackRange);
        fsm.AddTransition("Chase", "Search",
            t => distanceToPlayer > searchSpotRange,
            onTransition: t => lastSeenPlayerPosition = playerPosition);
        fsm.AddTransition("Search", "Chase", t => distanceToPlayer <= searchSpotRange);
        fsm.AddTransition(new TransitionAfter("Search", "Patrol", searchTime));

        fsm.Init();
    }

    void Update()
    {
        fsm.OnLogic();
        stateDisplayText.text = fsm.GetActiveHierarchyPath();
    }

    // Triggers the `PlayerSpotted` event.
    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player"))
        {
            fsm.Trigger("PlayerSpotted");
        }
    }

    private void MoveTowards(Vector2 target, float speed, float minDistance=0)
    {
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            Mathf.Max(0, Mathf.Min(speed * Time.deltaTime, Vector2.Distance(transform.position, target) - minDistance))
        );
    }

    private IEnumerator MoveToPosition(Vector2 target, float speed, float tolerance=0.05f)
    {
        while (Vector2.Distance(transform.position, target) > tolerance)
        {
            MoveTowards(target, speed);
            // Wait one frame.
            yield return null;
        }
    }

    private IEnumerator Patrol()
    {
        int currentPointIndex = FindClosestPatrolPoint();

        while (true)
        {
            yield return MoveToPosition(patrolPoints[currentPointIndex], patrolSpeed);

            // Wait at each patrol point.
            yield return new WaitForSeconds(3);

            currentPointIndex += patrolDirection;

            // Once the bot reaches the end or the beginning of the patrol path,
            // it reverses the direction.
            if (currentPointIndex >= patrolPoints.Length || currentPointIndex < 0)
            {
                currentPointIndex = Mathf.Clamp(currentPointIndex, 0, patrolPoints.Length-1);
                patrolDirection *= -1;
            }
        }
    }

    private int FindClosestPatrolPoint()
    {
        float minDistance = Vector2.Distance(transform.position, patrolPoints[0]);
        int minIndex = 0;

        for (int i = 1; i < patrolPoints.Length; i ++)
        {
            float distance = Vector2.Distance(transform.position, patrolPoints[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                minIndex = i;
            }
        }

        return minIndex;
    }

    private IEnumerator Search()
    {
        yield return MoveToPosition(lastSeenPlayerPosition, chaseSpeed);

        while (true)
        {
            yield return new WaitForSeconds(2);

            yield return MoveToPosition(
                (Vector2)transform.position + Random.insideUnitCircle * 10,
                patrolSpeed
            );
        }
    }
}

}