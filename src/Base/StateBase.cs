
namespace FSM
{
	/// <summary>
	/// The base class of all states
	/// </summary>
	public class StateBase<TStateId>
	{
		public bool needsExitTime;
		public TStateId name;

		public IStateMachine<TStateId> fsm;

		/// <summary>
		/// Initialises a new instance of the BaseState class
		/// </summary>
		/// <param name="needsExitTime">Determins if the state is allowed to instantly
		/// 	exit on a transition (false), or if the state machine should wait until
		/// 	the state is ready for a state change (true)</param>
		public StateBase(bool needsExitTime)
		{
			this.needsExitTime = needsExitTime;
		}

		/// <summary>
		/// Called to initialise the state, after values like name, mono and fsm have been set
		/// </summary>
		public virtual void Init()
		{

		}

		/// <summary>
		/// Called when the state machine transitions to this state (enters this state)
		/// </summary>
		public virtual void OnEnter()
		{

		}

		/// <summary>
		/// Called while this state is active
		/// </summary>
		public virtual void OnLogic() {

		}

		/// <summary>
		/// Called when the state machine transitions from this state to another state (exits this state)
		/// </summary>
		public virtual void OnExit()
		{

		}

		/// <summary>
		/// (Only if needsExitTime is true):
		/// 	Called when a state transition from this state to another state should happen.
		/// 	If it can exit, it should call fsm.StateCanExit()
		/// 	and if it can not exit right now, it should call fsm.StateCanExit() later in OnLogic().
		/// </summary>
		public virtual void OnExitRequest()
		{

		}
	}

	public class StateBase : StateBase<string>
	{
		public StateBase(bool needsExitTime) : base(needsExitTime)
		{
		}
	}
}
