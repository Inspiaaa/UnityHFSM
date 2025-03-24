using System;
using System.Collections.Generic;

namespace UnityHFSM.Visualization
{
	public abstract class StateMachinePath : IEquatable<StateMachinePath>
	{
		public static readonly StateMachinePath Root = new RootStateMachinePath();

		public readonly StateMachinePath parentPath;

		public bool IsRoot => this is RootStateMachinePath;

		protected StateMachinePath(StateMachinePath parentPath) {
			this.parentPath = parentPath;
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
	}

	public class StateMachinePath<TStateId> : StateMachinePath, IEquatable<StateMachinePath<TStateId>>
	{
		public readonly TStateId name;

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

	public class RootStateMachinePath : StateMachinePath, IEquatable<RootStateMachinePath>
	{
		public RootStateMachinePath() : base(null) { }

		public override int GetHashCode()
		{
			return "Root".GetHashCode();
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
			return "Root";
		}
	}
}
