using UnityEngine;

namespace UnityHFSM.Tests
{
	/// <summary>
	/// Default timer that calculates the elapsed time based on Time.time.
	/// </summary>
	public class TestTimer : ITimer
	{
		public float Elapsed { get; set; }

		public void Reset()
		{
			Elapsed = 0;
		}
	}
}
