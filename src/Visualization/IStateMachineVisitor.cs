namespace UnityHFSM.Visualization
{
	public interface IStateMachineVisitor
	{
		void VisitStateMachine<TOwnId, TStateId, TEvent>(
			StateMachinePath fsmPath,
			StateMachine<TOwnId, TStateId, TEvent> fsm);

		void VisitRegularChildState<TParentId, TStateId, TEvent>(
			StateMachinePath parentPath,
			StateMachine<TParentId, TStateId, TEvent> parentFsm,
			StateMachinePath childPath,
			StateBase<TStateId> state);

		void VisitChildStateMachine<TParentId, TOwnId, TStateId, TEvent>(
			StateMachinePath parentPath,
            StateMachine<TParentId, TOwnId, TEvent> parentFsm,
			StateMachinePath childPath,
            StateMachine<TOwnId, TStateId, TEvent> fsm);

		/// <summary>
		/// Called after the current state machine and all its child states (and child state machines) have
		/// been visited.
		/// </summary>
		void ExitStateMachine<TOwnId, TStateId, TEvent>(
			StateMachinePath fsmPath,
			StateMachine<TOwnId, TStateId, TEvent> fsm);
	}
}
