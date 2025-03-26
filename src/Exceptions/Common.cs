
using UnityHFSM.Inspection;

namespace UnityHFSM.Exceptions
{
	public static class Common
	{
		public static StateMachineException NotInitialized<TStateId>(
			StateBase<TStateId> fsm,
			string context = null,
			string problem = null,
			string solution = null)
		{
			return CreateStateMachineException(
				fsm,
				context,
				problem ?? "The active state is null because the state machine has not been set up yet.",
				solution ?? ("Call fsm.SetStartState(...) and fsm.Init() or fsm.OnEnter() "
					+ "to initialize the state machine.")
			);
		}

		public static StateMachineException StateNotFound<TStateId>(
			StateBase<TStateId> fsm,
			string stateName,
			string context = null,
			string problem = null,
			string solution = null)
		{
			return CreateStateMachineException(
				fsm,
				context,
				problem ?? $"The state \"{stateName}\" has not been defined yet / doesn't exist.",
				solution ?? ("\n"
					+ "1. Check that there are no typos in the state names and transition from and to names\n"
					+ "2. Add this state before calling Init / OnEnter / OnLogic / RequestStateChange / ...")
			);
		}

		public static StateMachineException MissingStartState<TStateId>(
			StateBase<TStateId> fsm,
			string context = null,
			string problem = null,
			string solution = null)
		{
			return CreateStateMachineException(
				fsm,
				context,
				problem ?? ("No start state is selected. "
					+ "The state machine needs at least one state to function properly."),
				solution ?? ("Make sure that there is at least one state in the state machine "
					+ "before running Init() or OnEnter() by calling fsm.AddState(...).")
			);
		}

		public static StateMachineException QuickIndexerMisusedForGettingState<TStateId>(
			StateBase<TStateId> fsm,
			string stateName)
		{
			return CreateStateMachineException(
				fsm,
				context: "Getting a nested state machine with the indexer",
				problem: "The selected state is not a state machine.",
				solution: ("This method is only there for quickly accessing a nested state machine. "
					+ $"To get the selected state, use GetState(\"{stateName}\")")
			);
		}

		private static StateMachineException CreateStateMachineException<TStateId>(
			StateBase<TStateId> fsm,
			string context = null,
			string problem = null,
			string solution = null)
		{
			string path = StateMachineWalker.GetStringPathOfState(fsm);

			return new StateMachineException(ExceptionFormatter.Format(
				location: $"state machine '{path}'",
				context: context,
				problem: problem,
				solution: solution
			));
		}
	}
}
