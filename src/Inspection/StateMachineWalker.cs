
using System.Collections.Generic;

namespace UnityHFSM.Inspection
{
	/// <summary>
	/// Utility class for methods that traverse a hierarchical state machine.
	/// </summary>
	public class StateMachineWalker
	{
		/// <summary>
		/// Recursively walks through the hierarchy, visiting every state.
		/// </summary>
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
		/// Extracts the path to the active state.
		/// </summary>
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
		/// Extracts the path to a given state from the root.
		/// </summary>
		private class StatePathExtractor<TStartStateId> : IStateVisitor
		{
			public StateMachinePath path;

			public StatePathExtractor(StateBase<TStartStateId> state)
			{
				VisitParent(state.fsm);
				state.AcceptVisitor(this);
			}

			private void VisitParent(IStateTimingManager parent)
			{
				if (parent == null)
					return;

				// Construct the path to the parent of this state (from root).
				VisitParent(parent.ParentFsm);
				// Add this state to the path.
				(parent as IVisitableState)?.AcceptVisitor(this);
			}

			public void VisitStateMachine<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm)
			{
				if (fsm.IsRootFsm)
				{
					path = StateMachinePath.Root;
				}
				else
				{
					AddToPath(fsm.name);
				}
			}

			public void VisitRegularState<TStateId>(StateBase<TStateId> state)
			{
				AddToPath(state.name);
			}

			private void AddToPath<TStateId>(TStateId name)
			{
				path = path == null
					? new StateMachinePath<TStateId>(name)
					: new StateMachinePath<TStateId>(path, name);
			}
		}

		/// <summary>
		/// An optimised variant of the <see cref="StatePathExtractor{TStartStateId}"/> when only
		/// a string path is needed.
		/// </summary>
		private class StringStatePathExtractor<TStartStateId> : IStateVisitor
		{
			public string path;

			public StringStatePathExtractor(StateBase<TStartStateId> state)
			{
				VisitParent(state.fsm);
				state.AcceptVisitor(this);
			}

			private void VisitParent(IStateTimingManager parent)
			{
				if (parent == null)
					return;

				// Construct the path to the parent of this state (from root).
				VisitParent(parent.ParentFsm);
				// Add this state to the path.
				(parent as IVisitableState)?.AcceptVisitor(this);
			}

			public void VisitStateMachine<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm)
			{
				if (fsm.IsRootFsm)
				{
					path = RootStateMachinePath.name;
				}
				else
				{
					AddToPath(fsm.name);
				}
			}

			public void VisitRegularState<TStateId>(StateBase<TStateId> state)
			{
				AddToPath(state.name);
			}

			private void AddToPath<TStateId>(TStateId name)
			{
				path = path == null
					? name.ToString()
					: path + "/" + name;
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

		/// <summary>
		/// Gets the path to the lowermost active state in the hierarchy.
		/// </summary>
		public static StateMachinePath GetActiveStatePath<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm)
		{
			var visitor = new ActiveStateVisitor();
			visitor.VisitStateMachine(fsm);
			return visitor.activePath;
		}

		/// <summary>
		/// Gets the path from the root state machine to the given state.
		/// </summary>
		public static StateMachinePath GetPathOfState<TStateId>(StateBase<TStateId> state)
		{
			var visitor = new StatePathExtractor<TStateId>(state);
			return visitor.path;
		}

		/// <summary>
		/// Optimised variant of the <see cref="GetPathOfState{TStateId}"/> method that returns
		/// a string path from the root state machine to the given state.
		/// </summary>
		public static string GetStringPathOfState<TStateId>(StateBase<TStateId> state)
		{
			var visitor = new StringStatePathExtractor<TStateId>(state);
			return visitor.path;
		}
	}
}
