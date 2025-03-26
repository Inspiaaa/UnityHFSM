
namespace UnityHFSM.Exceptions
{
	public static class ExceptionFormatter
	{
		public static string Format(
			string context = null,
			string problem = null,
			string solution = null)
		{
			return Format(
				location: null,
				context: context,
				problem: problem,
				solution: solution
			);
		}

		public static string Format(
			string location,
			string context,
			string problem,
			string solution)
		{
			string message = "\n";

			if (location != null)
			{
				message += "In " + location + "\n";
			}

			if (context != null)
			{
				message += "Context: " + context + "\n";
			}

			if (problem != null)
			{
				message += "Problem: " + problem + "\n";
			}

			if (solution != null)
			{
				message += "Solution: " + solution + "\n";
			}

			return message;
		}
	}
}
