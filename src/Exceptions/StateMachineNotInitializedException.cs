using System;

namespace FSM.Exceptions
{
	[Serializable]
	public class StateMachineNotInitializedException : Exception
	{
		public static string Format(
			string context,
			string problem,
			string solution)
		{
			if (problem == null)
			{
				problem = "The active state is null because the state machine has not been set up yet.";
			}

			if (solution == null)
			{
				solution = "Call fsm.SetStartState(...) and fsm.Init() or fsm.OnEnter() "
					+ "to initialize the state machine.";
			}

			return ExceptionFormatter.Format(context, problem, solution);
		}

		public StateMachineNotInitializedException(
			string context = null,
			string problem = null,
			string solution = null) : base(Format(context, problem, solution))
		{ }
	}
}
