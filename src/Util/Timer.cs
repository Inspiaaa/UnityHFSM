using UnityEngine;

namespace FSM {
	public class Timer {
		public float startTime;
		public float Elapsed => Time.time - startTime;

		public Timer() {
			startTime = Time.time;
		}

		public void Reset() {
			startTime = Time.time;
		}

		public static bool operator > (Timer timer, float duration) {
			return timer.Elapsed > duration;
		}

		public static bool operator < (Timer timer, float duration) {
			return timer.Elapsed < duration;
		}

		public static bool operator >= (Timer timer, float duration) {
			return timer.Elapsed >= duration;
		}

		public static bool operator <= (Timer timer, float duration) {
			return timer.Elapsed <= duration;
		}
	}
}
