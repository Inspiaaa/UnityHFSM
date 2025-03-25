
namespace UnityHFSM.Inspection
{
	/// <summary>
	/// Utility class for methods that traverse a hierarchical state machine.
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

		private class ActiveStateVisitor : IStateVisitor
		{
			public StateMachinePath activePath;

			public void VisitStateMachine<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm)
			{
				activePath = activePath is null
					? StateMachinePath.Root
					: new StateMachinePath<TOwnId>(activePath, fsm.name);

				fsm.ActiveState.AcceptVisitor(this);
			}

			public void VisitRegularState<TStateId>(StateBase<TStateId> state)
			{
				activePath = new StateMachinePath<TStateId>(activePath, state.name);
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

		public static StateMachinePath GetActiveStatePath<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm)
		{
			var visitor = new ActiveStateVisitor();
			visitor.VisitStateMachine(fsm);
			return visitor.activePath;
		}
	}
}
