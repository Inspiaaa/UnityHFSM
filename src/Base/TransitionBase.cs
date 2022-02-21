
namespace FSM
{
	/// <summary>
	/// The base class of all transitions
	/// </summary>
	public class TransitionBase<TStateId>
	{
		public TStateId from;
		public TStateId to;

		public bool forceInstantly;

		public IStateMachine<TStateId> fsm;

		/// <summary>
		/// Initialises a new instance of the TransitionBase class
		/// </summary>
		/// <param name="from">The name / identifier of the active state</param>
		/// <param name="to">The name / identifier of the next state</param>
		/// <param name="forceInstantly">Ignores the needsExitTime of the active state if forceInstantly is true
		/// 	=> Forces an instant transition</param>
		public TransitionBase(TStateId from, TStateId to, bool forceInstantly = false)
		{
			this.from = from;
			this.to = to;
			this.forceInstantly = forceInstantly;
		}

		/// <summary>
		/// Called to initialise the transition, after values like mono and fsm have been set
		/// </summary>
		public virtual void Init()
		{

		}

		/// <summary>
		/// Called when the state machine enters the "from" state
		/// </summary>
		public virtual void OnEnter()
		{

		}

		/// <summary>
		/// Called to determin whether the state machine should transition to the <c>to</c> state
		/// </summary>
		/// <returns>True if the state machine should change states / transition</returns>
		public virtual bool ShouldTransition()
		{
			return true;
		}
	}

	public class TransitionBase : TransitionBase<string>
	{
		public TransitionBase(string @from, string to, bool forceInstantly = false) : base(@from, to, forceInstantly)
		{
		}
	}
}
