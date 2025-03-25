namespace UnityHFSM.Inspection
{
	/// <summary>
	/// Defines the interface for a visitor that can perform operations on different states
	/// of the state machine. This is part of the Visitor Pattern, which allows new behavior
	/// to be added to existing state classes without modifying their code. It is used to
	/// implement dynamic inspection tools for hierarchical state machines.
	/// </summary>
	public interface IStateVisitor
	{
		void VisitStateMachine<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm);

		void VisitRegularState<TStateId>(StateBase<TStateId> state);
	}
}
