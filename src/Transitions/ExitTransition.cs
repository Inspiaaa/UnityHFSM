namespace FSM
{
	/// <summary>
	/// An ExitTransition wraps another transition.
    /// It ensures that the transition, which represents an "exit transition", i.e. a vertical
    /// transition that allows the fsm to exit, can only occur if the parent state machine
    /// of the fsm has a pending transition.
	/// </summary>
    /// <remarks>
    /// This is an internal class designed to implement the exit transitions feature.
    /// </remarks>
	public class ExitTransition<TStateId> : TransitionBase<TStateId>
	{
		public TransitionBase<TStateId> wrappedTransition;
		private bool shouldInitWrappedTransition;

		public ExitTransition(
				TransitionBase<TStateId> wrappedTransition,
				bool shouldInitWrappedTransition = true)
			: base(
				from: wrappedTransition.from,
				to: default,
				forceInstantly: wrappedTransition.forceInstantly)
		{
			this.wrappedTransition = wrappedTransition;
			this.shouldInitWrappedTransition = shouldInitWrappedTransition;
			this.isExitTransition = true;
		}

		public override void Init()
		{
			if (shouldInitWrappedTransition)
			{
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
            IStateMachine parentFsm = fsm.ParentFsm;

			return parentFsm != null
                && parentFsm.HasPendingTransition
                && wrappedTransition.ShouldTransition();
		}
	}

	public class ExitTransition : ExitTransition<string>
	{
		public ExitTransition(
			TransitionBase<string> wrappedTransition,
			bool shouldInitWrappedTransition = true)
			: base(wrappedTransition, shouldInitWrappedTransition)
		{
		}
	}
}