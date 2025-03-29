namespace UnityHFSM
{
	/// <summary>
	/// A ReverseTransition wraps another transition, but reverses it. The "from"
	/// and "to" states are swapped. Only when the condition of the wrapped transition
	/// is false does it transition.
	/// The <c>BeforeTransition</c> and <c>AfterTransition</c> callbacks of the wrapped transition
	/// are also swapped.
	/// </summary>
	public class ReverseTransition<TStateId> : TransitionBase<TStateId>
	{
		public readonly TransitionBase<TStateId> wrappedTransition;
		private readonly bool shouldInitWrappedTransition;

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
			return !wrappedTransition.ShouldTransition();
		}

		public override void BeforeTransition()
		{
			wrappedTransition.AfterTransition();
		}

		public override void AfterTransition()
		{
			wrappedTransition.BeforeTransition();
		}
	}

	/// <inheritdoc />
	public class ReverseTransition : ReverseTransition<string>
	{
		/// <inheritdoc />
		public ReverseTransition(
			TransitionBase<string> wrappedTransition,
			bool shouldInitWrappedTransition = true)
			: base(wrappedTransition, shouldInitWrappedTransition)
		{
		}
	}
}
