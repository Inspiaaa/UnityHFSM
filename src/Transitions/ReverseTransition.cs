namespace FSM
{
	public class ReverseTransition<TStateId> : TransitionBase<TStateId>
	{
		public TransitionBase<TStateId> wrappedTransition;
		private bool shouldInitWrappedTransition;

		public ReverseTransition(
				TransitionBase<TStateId> wrappedTransition,
				bool shouldInitWrappedTransition = true)
			: base(
				from: wrappedTransition.to,
				to: wrappedTransition.from,
				forceInstantly: wrappedTransition.forceInstantly)
		{
			this.wrappedTransition = wrappedTransition;
			this.shouldInitWrappedTransition = shouldInitWrappedTransition;
		}

		public override void Init()
		{
			if (shouldInitWrappedTransition) {
				wrappedTransition.fsm = this.fsm;
				wrappedTransition.Init();
			}
		}

		public override void OnEnter()
		{
			wrappedTransition.OnEnter();
		}

		public override bool ShouldTransition()
		{
			return ! wrappedTransition.ShouldTransition();
		}

	}
}