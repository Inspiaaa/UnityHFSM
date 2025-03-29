
using UnityHFSM.Inspection;

namespace UnityHFSM
{
	/// <summary>
	/// The base class of all states.
	/// </summary>
	public class StateBase<TStateId> : IVisitableState
	{
		public readonly bool needsExitTime;
		public readonly bool isGhostState;
		public TStateId name;

		public IStateTimingManager fsm;

		/// <summary>
		/// Initialises a new instance of the StateBase class.
		/// </summary>
		/// <param name="needsExitTime">Determines if the state is allowed to instantly
		/// 	exit on a transition (false), or if the state machine should wait until
		/// 	the state is ready for a state change (true).</param>
		/// <param name="isGhostState">If true, this state becomes a ghost state, a
		/// 	state the state machine does not want to stay in. That means that if the
		/// 	fsm transitions to this state, it will test all outgoing transitions instantly
		/// 	and not wait until the next OnLogic call.</param>
		public StateBase(bool needsExitTime, bool isGhostState = false)
		{
			this.needsExitTime = needsExitTime;
			this.isGhostState = isGhostState;
		}

		/// <summary>
		/// Called to initialise the state, after values like <c>name</c> and <c>fsm</c> have been set.
		/// </summary>
		public virtual void Init()
		{

		}

		/// <summary>
		/// Called when the state machine transitions to this state (enters this state).
		/// </summary>
		public virtual void OnEnter()
		{

		}

		/// <summary>
		/// Called while this state is active.
		/// </summary>
		public virtual void OnLogic()
		{

		}

		/// <summary>
		/// Called when the state machine transitions from this state to another state (exits this state).
		/// </summary>
		public virtual void OnExit()
		{

		}

		/// <summary>
		/// (Only if <c>needsExitTime</c> is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call <c>fsm.StateCanExit()</c>
		/// 	and if it can not exit right now, it should call <c>fsm.StateCanExit()</c> later
		///		in e.g. <c>OnLogic()</c>.
		/// </summary>
		public virtual void OnExitRequest()
		{

		}

		/// <summary>
		/// Returns a string representation of all active states in the hierarchy,
		/// e.g. <c>"/Move/Jump/Falling"</c>.
		/// In contrast, the state machine's <c>ActiveStateName</c> property only returns the name
		/// of its active state, not of any nested states.
		/// </summary>
		public virtual string GetActiveHierarchyPath()
		{
			return name.ToString();
		}

		/// <summary>
		/// Accepts a visitor that can perform operations on the current state.
		/// This method is part of the Visitor Pattern, which allows adding new behavior
		/// to state machine states without modifying their class structure. It is used to
		/// implement dynamic inspection tools for hierarchical state machines.
		/// </summary>
		public virtual void AcceptVisitor(IStateVisitor visitor)
		{
			visitor.VisitRegularState(this);
		}
	}

	/// <inheritdoc />
	public class StateBase : StateBase<string>
	{
		/// <inheritdoc />
		public StateBase(bool needsExitTime, bool isGhostState = false)
			: base(needsExitTime, isGhostState)
		{
		}
	}
}
