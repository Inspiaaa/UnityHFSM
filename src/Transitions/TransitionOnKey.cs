using UnityEngine;

namespace UnityHFSM
{
	/// <summary>
	/// Built-in keyboard-related transition types to increase readability and reduce boilerplate.
	/// </summary>
	public static class TransitionOnKey
	{
		/// <summary>
		/// A transition type that triggers while a key is down.
		/// It behaves like <c>Input.GetKey(...)</c>.
		/// </summary>
		public class Down<TStateId> : TransitionBase<TStateId>
		{
			private readonly KeyCode keyCode;

			/// <param name="key">The <c>KeyCode</c> of the key to watch.</param>
			public Down(
					TStateId from,
					TStateId to,
					KeyCode key,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				keyCode = key;
			}

			public override bool ShouldTransition()
			{
				return Input.GetKey(keyCode);
			}
		}

		/// <summary>
		/// A transition type that triggers when a key was just down and is up now.
		/// It behaves like <c>Input.GetKeyUp(...)</c>.
		/// </summary>
		/// <typeparam name="TStateId"></typeparam>
		public class Released<TStateId> : TransitionBase<TStateId>
		{
			private readonly KeyCode keyCode;

			/// <param name="key">The <c>KeyCode</c> of the key to watch.</param>
			public Released(
					TStateId from,
					TStateId to,
					KeyCode key,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				keyCode = key;
			}

			public override bool ShouldTransition()
			{
				return Input.GetKeyUp(keyCode);
			}
		}

		/// <summary>
		/// A transition type that triggers when a key was just up and is down now.
		/// It behaves like <c>Input.GetKeyDown(...)</c>.
		/// </summary>
		public class Pressed<TStateId> : TransitionBase<TStateId>
		{
			private readonly KeyCode keyCode;

			/// <param name="key">The <c>KeyCode</c> of the key to watch.</param>
			public Pressed(
					TStateId from,
					TStateId to,
					KeyCode key,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				keyCode = key;
			}

			public override bool ShouldTransition()
			{
				return Input.GetKeyDown(keyCode);
			}
		}

		/// <summary>
		/// A transition type that triggers while a key is up.
		/// It behaves like <c>!Input.GetKey(...)</c>.
		/// </summary>
		public class Up<TStateId> : TransitionBase<TStateId>
		{
			private readonly KeyCode keyCode;

			/// <param name="key">The <c>KeyCode</c> of the key to watch.</param>
			public Up(
					TStateId from,
					TStateId to,
					KeyCode key,
					bool forceInstantly = false) : base(from, to, forceInstantly)
			{
				keyCode = key;
			}

			public override bool ShouldTransition()
			{
				return !Input.GetKey(keyCode);
			}
		}

		/// <inheritdoc />
		public class Down : Down<string>
		{
			public Down(
				string @from,
				string to,
				KeyCode key,
				bool forceInstantly = false) : base(@from, to, key, forceInstantly)
			{
			}
		}

		/// <inheritdoc />
		public class Released : Released<string>
		{
			public Released(
				string @from,
				string to,
				KeyCode key,
				bool forceInstantly = false) : base(@from, to, key, forceInstantly)
			{
			}
		}

		/// <inheritdoc />
		public class Pressed : Pressed<string>
		{
			public Pressed(
				string @from,
				string to,
				KeyCode key,
				bool forceInstantly = false) : base(@from, to, key, forceInstantly)
			{
			}
		}

		/// <inheritdoc />
		public class Up : Up<string>
		{
			public Up(
				string @from,
				string to,
				KeyCode key,
				bool forceInstantly = false) : base(@from, to, key, forceInstantly)
			{
			}
		}
	}
}
