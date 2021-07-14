using System;

namespace FSM.Exceptions
{
	[Serializable]
	public class StateNotFoundException<TState> : Exception
	{
		private static string Format(
			TState stateName,
			string context,
			string problem,
			string solution)
		{
			if (problem == null)
			{
				problem = $"The state \"{stateName}\" has not been defined yet / doesn't exist.";
			}

			if (solution == null)
			{
				solution = "\n"
					+ "1. Check that there are no typos in the state names and transition from and to names\n"
					+ "2. Add this state before calling Init / OnEnter / OnLogic / RequestStateChange / ...";
			}

			return ExceptionFormatter.Format(context, problem, solution);
		}

		public StateNotFoundException(
			TState stateName,
			string context = null,
			string problem = null,
			string solution = null) : base(Format(stateName, context, problem, solution))
		{ }
	}

	public class StateNotFoundException : StateNotFoundException<string>
	{
		public StateNotFoundException(
			string stateName,
			string context = null,
			string problem = null,
			string solution = null) : base(stateName, context, problem, solution)
		{
		}
	}
}
