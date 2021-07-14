
namespace FSM
{
	/// <summary>
	/// A subset of features that every parent state machine has to provide.
	/// It is useful, as it allows the parent state machine to be independent from the
	/// sub-states. This becomes very obvious when there isn't such an abstraction, as
	/// every sub state would have to provide all generic type parameters of the fsm.
	/// => An abstraction layer
	/// </summary>
	/// <typeparam name="TStateId">They type of the names / ids of the sub states</typeparam>
	public interface IStateMachine<TStateId>
	{
		/// <summary>
		/// Tells the state machine that, if there is a state transition pending,
		/// now is the time to perform it.
		/// </summary>
		void StateCanExit();

		void RequestStateChange(TStateId name, bool forceInstantly = false);

		StateBase<TStateId> ActiveState { get; }
		TStateId ActiveStateName { get; }
	}
}
