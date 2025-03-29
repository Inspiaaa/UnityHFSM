using UnityEngine;

namespace UnityHFSM
{
	/// <summary>
	/// Built-in mouse-related transition types to increase readability and reduce boilerplate.
	/// </summary>
	public static class TransitionOnMouse
	{
		/// <summary>
		/// A transition type that triggers while a mouse button is down.
		/// It behaves like <c>Input.GetMouseButton(...)</c>.
		/// </summary>
		public class Down<TStateId> : TransitionBase<TStateId>
		{
			private readonly int button;

			/// <param name="button">The mouse button to watch.</param>
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

		/// <summary>
		/// A transition type that triggers when a mouse button was just down and is up now.
		/// It behaves like <c>Input.GetMouseButtonUp(...)</c>.
		/// </summary>
		public class Released<TStateId> : TransitionBase<TStateId>
		{
			private readonly int button;

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

		/// <summary>
		/// A transition type that triggers when a mouse button was just up and is down now.
		/// It behaves like <c>Input.GetMouseButtonDown(...)</c>.
		/// </summary>
		public class Pressed<TStateId> : TransitionBase<TStateId>
		{
			private readonly int button;

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

		/// <summary>
		/// A transition type that triggers while a mouse button is up.
		/// It behaves like <c>!Input.GetMouseButton(...)</c>.
		/// </summary>
		public class Up<TStateId> : TransitionBase<TStateId>
		{
			private readonly int button;

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

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
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

		/// <inheritdoc />
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
