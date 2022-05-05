using System;

namespace FSM
{
	public class ReverseTransition<TStateId> : TransitionBase<TStateId>
	{
		public TransitionBase<TStateId> wrappedTransition;

		public ReverseTransition(TransitionBase<TStateId> wrappedTransition) : base(
			from: wrappedTransition.to,
			to: wrappedTransition.from,
			forceInstantly: wrappedTransition.forceInstantly)
		{
			this.wrappedTransition = wrappedTransition;
		}

		public override bool ShouldTransition()
		{
			return ! wrappedTransition.ShouldTransition();
		}

		public override void OnEnter()
		{
			wrappedTransition.OnEnter();
		}
	}
}