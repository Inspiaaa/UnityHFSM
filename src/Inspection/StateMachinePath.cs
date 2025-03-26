using System;
using System.Collections.Generic;

namespace UnityHFSM.Inspection
{
	/// <summary>
	/// Light-weight, hashable and equatable type that represents the path to a state within a hierarchical
	/// state machine. It supports different state ID types for each level. Each instance represents a node
	/// that is linked to the <c>StateMachinePath</c> of its parent node.
	/// </summary>
	/// <remarks>
	/// In contrast to string-based paths, this type does not suffer from accidental naming-collisions
	/// when "magic characters" are used in state names.
	/// </remarks>
	public abstract class StateMachinePath : IEquatable<StateMachinePath>
	{
		public static readonly StateMachinePath Root = RootStateMachinePath.instance;

		public readonly StateMachinePath parentPath;

		public bool IsRoot => this is RootStateMachinePath;

		public abstract string LastNodeName { get; }

		protected StateMachinePath(StateMachinePath parentPath) {
			this.parentPath = parentPath;
		}

		public bool IsChildPathOf(StateMachinePath parent)
		{
			for (StateMachinePath ancestor = parentPath; ancestor != null; ancestor = ancestor.parentPath)
			{
				if (ancestor == parent)
				{
					return true;
				}
			}

			return false;
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as StateMachinePath);
		}

		public abstract override int GetHashCode();
		public abstract bool Equals(StateMachinePath other);
		public abstract override string ToString();

		public static bool operator ==(StateMachinePath left, StateMachinePath right)
		{
			if (left is null && right is null)
				return true;
			if (left is null)
				return false;
			return left.Equals(right);
		}

		public static bool operator !=(StateMachinePath left, StateMachinePath right)
		{
			return !(left == right);
		}

		public StateMachinePath Join<TStateId>(TStateId name)
		{
			return new StateMachinePath<TStateId>(this, name);
		}
	}

	/// <summary>
	/// Represents a state / state machine within a <see cref="StateMachinePath"/>.
	/// </summary>
	public class StateMachinePath<TStateId> : StateMachinePath, IEquatable<StateMachinePath<TStateId>>
	{
		public readonly TStateId name;

		public override string LastNodeName => name.ToString();

		public StateMachinePath(TStateId name) : base(null)
		{
			this.name = name;
		}

		public StateMachinePath(StateMachinePath parentPath, TStateId name) : base(parentPath)
		{
			this.name = name;
		}

		public override string ToString()
		{
			return (parentPath?.ToString() ?? "") + "/" + name.ToString();
		}

		public override bool Equals(StateMachinePath path)
		{
			return Equals(path as StateMachinePath<TStateId>);
		}

		public bool Equals(StateMachinePath<TStateId> other)
		{
			if (other is null)
				return false;

			if (this.parentPath is null && other.parentPath is not null)
				return false;

			if (this.parentPath is not null && other.parentPath is null)
				return false;

			if (!EqualityComparer<TStateId>.Default.Equals(this.name, other.name))
				return false;

			return this.parentPath?.Equals(other.parentPath) ?? true;
		}

		public override int GetHashCode()
		{
			int ownHash = EqualityComparer<TStateId>.Default.GetHashCode(name);

			return parentPath is null
				? ownHash
				: HashCode.Combine(parentPath.GetHashCode(), ownHash);
		}
	}

	/// <summary>
	/// Represents the <see cref="StateMachinePath"/> of the root state machine.
	/// </summary>
	public class RootStateMachinePath : StateMachinePath, IEquatable<RootStateMachinePath>
	{
		public const string name = "Root";
		public static readonly RootStateMachinePath instance = new RootStateMachinePath();

		public override string LastNodeName => name;

		private RootStateMachinePath() : base(null) { }

		public override int GetHashCode()
		{
			return name.GetHashCode();
		}

		public override bool Equals(StateMachinePath other)
		{
			return Equals(other as RootStateMachinePath);
		}

		public bool Equals(RootStateMachinePath other)
		{
			return other is not null;
		}

		public override string ToString()
		{
			return name;
		}
	}
}
