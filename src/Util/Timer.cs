using UnityEngine;

namespace FSM
{
	/// <summary>
	/// Default timer that calculates the elapsed time based on Time.time.
	/// </summary>
	public class Timer : ITimer
	{
		public float startTime;
		public float Elapsed => Time.time - startTime;

		public Timer()
		{
			startTime = Time.time;
		}

		public void Reset()
		{
			startTime = Time.time;
		}

		public static bool operator >(Timer timer, float duration)
			=> timer.Elapsed > duration;

		public static bool operator <(Timer timer, float duration)
			=> timer.Elapsed < duration;

		public static bool operator >=(Timer timer, float duration)
			=> timer.Elapsed >= duration;

		public static bool operator <=(Timer timer, float duration)
			=> timer.Elapsed <= duration;
	}
}
