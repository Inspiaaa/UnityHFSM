using UnityEngine;

namespace UnityHFSM
{
	public static class TransitionOnMouse
	{
		public class Down<TStateId> : TransitionBase<TStateId>
		{
			private int button;

			/// <summary>
			/// Initialises a new transition that triggers, while a mouse button is down.
			/// It behaves like Input.GetMouseButton(...).
			/// </summary>
			/// <param name="button">The mouse button to watch.</param>
			/// <returns></returns>
			public Down(
					TStateId from,
					TStateId to,
					int button,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				this.button = button;
			}

			public override bool ShouldTransition()
			{
				return Input.GetMouseButton(button);
			}
		}

		public class Released<TStateId> : TransitionBase<TStateId>
		{
			private int button;

			/// <summary>
			/// Initialises a new transition that triggers, when a mouse button was just down and is up now.
			/// It behaves like Input.GetMouseButtonUp(...).
			/// </summary>
			/// <param name="button">The mouse button to watch.</param>
			public Released(
					TStateId from,
					TStateId to,
					int button,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				this.button = button;
			}

			public override bool ShouldTransition()
			{
				return Input.GetMouseButtonUp(button);
			}
		}

		public class Pressed<TStateId> : TransitionBase<TStateId>
		{
			private int button;

			/// <summary>
			/// Initialises a new transition that triggers, when a mouse button was just up and is down now.
			/// It behaves like Input.GetMouseButtonDown(...).
			/// </summary>
			/// <param name="button">The mouse button to watch.</param>
			public Pressed(
					TStateId from,
					TStateId to,
					int button,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				this.button = button;
			}

			public override bool ShouldTransition()
			{
				return Input.GetMouseButtonDown(button);
			}
		}

		public class Up<TStateId> : TransitionBase<TStateId>
		{
			private int button;

			/// <summary>
			/// Initialises a new transition that triggers, while a mouse button is up.
			/// It behaves like !Input.GetMouseButton(...).
			/// </summary>
			/// <param name="button">The mouse button to watch.</param>
			public Up(
					TStateId from,
					TStateId to,
					int button,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				this.button = button;
			}

			public override bool ShouldTransition()
			{
				return !Input.GetMouseButton(button);
			}
		}

		public class Down : Down<string>
		{
			public Down(
				string @from,
				string to,
				int button,
				bool forceInstantly = false) : base(@from, to, button, forceInstantly)
			{
			}
		}

		public class Released : Released<string>
		{
			public Released(
				string @from,
				string to,
				int button,
				bool forceInstantly = false) : base(@from, to, button, forceInstantly)
			{
			}
		}

		public class Pressed : Pressed<string>
		{
			public Pressed(
				string @from,
				string to,
				int button,
				bool forceInstantly = false) : base(@from, to, button, forceInstantly)
			{
			}
		}

		public class Up : Up<string>
		{
			public Up(
				string @from,
				string to,
				int button,
				bool forceInstantly = false) : base(@from, to, button, forceInstantly)
			{
			}
		}
	}
}
