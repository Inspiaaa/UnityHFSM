using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FSM
{
	/// <summary>
	/// use coroutine-like method to determin whether the state machine should transition to another state
	/// </summary>
	/// <typeparam name="TStateId"></typeparam>
	public class TransitionAwait<TStateId> : TransitionBase<TStateId>
    {
		private Stack<IEnumerator> stack;
		private IEnumerator routine;
		private Func<TransitionAwait<TStateId>, IEnumerator> condition;

		public TransitionAwait(
		TStateId from,
		TStateId to,
		Func<TransitionAwait<TStateId>, IEnumerator> condition,
		bool forceInstantly = false) : base(from, to, forceInstantly)
		{
			this.condition = condition;
		}
        public override bool ShouldTransition()
        {
			if (routine.MoveNext())
			{
				if (routine.Current is IEnumerator enumator)
                {
					stack.Push(routine);
					routine = enumator;
				}
				else if (routine.Current is bool value)
                {
					if (value) return true;
					else Reset();
				}
			}
            else
            {
				routine = stack.Count > 0 ? stack.Pop() : condition(this);
			}

			return false;
		}
        public override void Init()
        {
			stack = new Stack<IEnumerator>();
        }
        public override void OnEnter()
        {
			Reset();
		}
		private void Reset()
        {
			stack.Clear();
			routine = condition(this);
		}
		public IEnumerator WaitForSeconds(float seconds)
        {
			float enterTime = Time.timeSinceLevelLoad;
			while (Time.timeSinceLevelLoad - enterTime < seconds) yield return null;
        }
    }
	public class TransitionAwait : TransitionAwait<string>
	{
		public TransitionAwait(
			string @from,
			string to,
			Func<TransitionAwait<string>, IEnumerator> condition = null,
			bool forceInstantly = false) : base(@from, to, condition, forceInstantly)
		{
		}
	}
}
