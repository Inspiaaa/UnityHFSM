
namespace UnityHFSM
{
	/// <summary>
	/// An abstraction layer that provides a subset of features that every parent
	/// state machine has to provide in order to implement the timing mechanics of
	/// transitions. In addition to the methods provided by <see cref="IStateTimingManager"/>,
	/// this interface also provides access to the current and pending states, which
	/// can be useful for transitions.
	/// </summary>
	public interface IStateMachine<TStateId> : IStateTimingManager
	{
		/// <summary>
		/// The target state of a pending (delayed) transition. Returns null if no
		/// transition is pending or when an exit transition is pending.
		/// </summary>
		StateBase<TStateId> PendingState { get; }

		/// <inheritdoc cref="PendingState"/>
		TStateId PendingStateName { get; }

		/// <summary>
		/// The currently active state of the state machine.
		/// </summary>
		/// <remarks>
		/// Note that when a state is "active", the "ActiveState" may not return a reference to this state.
		/// Depending on the classes used, it may for example return a reference to a wrapper state.
		/// </remarks>
		StateBase<TStateId> ActiveState { get; }

		/// <inheritdoc cref="ActiveState"/>
		TStateId ActiveStateName { get; }

		StateBase<TStateId> GetState(TStateId name);
	}
}
