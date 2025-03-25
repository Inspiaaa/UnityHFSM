
namespace UnityHFSM.Inspection
{
	/// <summary>
	/// Class that can iterate over all states of a state machine. It recursively walks through the
	/// state machine hierarchy, calling the relevant methods of the provided <see cref="IStateMachineHierarchyVisitor"/>.
	/// </summary>
	public class StateMachineWalker
	{

		private class HierarchyWalker : IStateVisitor
		{
			private StateMachinePath path;
			private readonly IStateMachineHierarchyVisitor hierarchyVisitor;

			public HierarchyWalker(IStateMachineHierarchyVisitor hierarchyVisitor)
			{
				this.hierarchyVisitor = hierarchyVisitor;
			}

			public void VisitStateMachine<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm)
			{
				// Push this state machine on to the path.
				path = path is null
					? StateMachinePath.Root
					: new StateMachinePath<TOwnId>(path, fsm.name);

				hierarchyVisitor.VisitStateMachine(path, fsm);

				foreach (var state in fsm.GetAllStates())
				{
					state.AcceptVisitor(this);
				}

				hierarchyVisitor.ExitStateMachine(path, fsm);

				// Pop the state machine from the path.
				path = path.parentPath;
			}

			public void VisitRegularState<TStateId>(StateBase<TStateId> state)
			{
				var statePath = new StateMachinePath<TStateId>(path, state.name);
				hierarchyVisitor.VisitRegularState(statePath, state);
			}
		}

		/// <summary>
		/// Recursively traverses the state machine in a pre-order manner, calling the visitor methods
		/// on each state machine / child state / child fsm.
		/// </summary>
		public static void Walk<TOwnId, TStateId, TEvent>(
			StateMachine<TOwnId, TStateId, TEvent> fsm,
			IStateMachineHierarchyVisitor visitor)
		{
			new HierarchyWalker(visitor).VisitStateMachine(fsm);
		}
	}
}
