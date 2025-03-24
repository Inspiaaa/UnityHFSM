
using System;
using System.Reflection;

#if UNITY_EDITOR
namespace UnityHFSM.Visualization
{
	/// <summary>
	/// Class that can iterate over all states of a state machine. It recursively walks through the
	/// state machine hierarchy, calling the relevant methods of the provided <see cref="IStateMachineVisitor"/>.
	/// </summary>
	public class StateMachineWalker
	{
		private readonly IStateMachineVisitor visitor;
		private readonly MethodInfo visitChildStateMachineMethod;
		private readonly MethodInfo walkMethod;

		public StateMachineWalker(IStateMachineVisitor visitor)
		{
			this.visitor = visitor;
			this.visitChildStateMachineMethod = visitor.GetType().GetMethod(nameof(visitor.VisitChildStateMachine));
			this.walkMethod = this.GetType().GetMethod(nameof(Walk), BindingFlags.NonPublic | BindingFlags.Instance);
		}

		/// <summary>
		/// Recursively traverses the state machine in a pre-order manner, calling the visitor methods
		/// on each state machine / child state / child fsm.
		/// </summary>
		public void Walk<TOwnId, TStateId, TEvent>(StateMachine<TOwnId, TStateId, TEvent> fsm)
		{
			Walk(StateMachinePath.Root, fsm);
		}

		private void Walk<TOwnId, TStateId, TEvent>(StateMachinePath path, StateMachine<TOwnId, TStateId, TEvent> fsm)
		{
			visitor.VisitStateMachine(path, fsm);

			foreach (var originalState in fsm.GetAllStates())
			{
				// Unwrap the state.
				StateBase<TStateId> state = originalState;
				while (state is DecoratedState<TStateId, TEvent> decoratedState)
				{
					state = decoratedState.state;
				}

				var childPath = new StateMachinePath<TStateId>(path, state.name);

				// As a child state machine may use different generic type parameters (which are only known at runtime)
				// from the parent state machine, we cannot use "normal" casts / "is ..." expressions to see if a state
				// is a state machine. Instead, we have to use reflection.
				if (IsStateMachine(state.GetType(), out Type stateMachineType))
				{
					// Child parameters: <TOwnId, TStateId, TEvent>
					Type[] childFsmTypeParameters = stateMachineType.GetGenericArguments();
					Type childFsmStateIdType = childFsmTypeParameters[1];

					// Generic parameters: <TParentId, TOwnId (of child), TStateId (within child), TEvent>
					visitChildStateMachineMethod
						.MakeGenericMethod(typeof(TOwnId), typeof(TStateId), childFsmStateIdType, typeof(TEvent))
						.Invoke(visitor, new object[] { path, fsm, childPath, state });

					walkMethod
						.MakeGenericMethod(childFsmTypeParameters)
						.Invoke(this, new object[] { childPath, state });
				}
				else
				{
					visitor.VisitRegularChildState(path, fsm, childPath, state);
				}
			}

			visitor.ExitStateMachine(path, fsm);
		}

		// Code inspired by https://glacius.tmont.com/articles/determining-if-an-open-generic-type-isassignablefrom-a-type
		// (MIT license)
		private static bool IsStateMachine(Type type, out Type stateMachineType)
		{
			if (type == null)
			{
				stateMachineType = null;
				return false;
			}

			Type genericStateMachineType = typeof(StateMachine<,,>);

			if (type == genericStateMachineType
			    || (type.IsGenericType && type.GetGenericTypeDefinition() == genericStateMachineType))
			{
				stateMachineType = type;
				return true;
			}
			else if (IsStateMachine(type.BaseType, out stateMachineType))
			{
				return true;
			}
			else
			{
				stateMachineType = null;
				return false;
			}
		}
	}
}
#endif
