
namespace UnityHFSM.Exceptions
{
	public static class ExceptionFormatter
	{
		public static string Format(
			string context = null,
			string problem = null,
			string solution = null)
		{
			string message = "\n";

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
