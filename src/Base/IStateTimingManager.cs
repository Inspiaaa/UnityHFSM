namespace UnityHFSM
{
	/// <summary>
	/// An abstraction layer that provides a subset of features that every parent
	/// state machine has to provide in order to implement the timing mechanics of
	/// transitions.
	/// It is useful, as it allows the parent state machine to be interchangeable and independent
	/// of the implementation details of the child states.
	/// </summary>
	/// <remarks>
	///	In particular, this means that child states do not have to provide the full list
	/// of generic type parameters required by their parent state machine.
	/// Otherwise, hierarchical state machines with different types for each level would
	/// be impossible to implement.
	/// </remarks>
	public interface IStateTimingManager
	{
		/// <summary>
		/// Tells the state machine that, if there is a state transition pending,
		/// now is the time to perform it.
		/// </summary>
		void StateCanExit();

		bool HasPendingTransition { get; }

		IStateTimingManager ParentFsm { get; }
	}
}