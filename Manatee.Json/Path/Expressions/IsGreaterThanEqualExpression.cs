using System;

namespace Manatee.Json.Path.Expressions
{
	internal class IsGreaterThanEqualExpression<T> : ExpressionTreeBranch<T>, IEquatable<IsGreaterThanEqualExpression<T>>
	{
		protected override int BasePriority => 1;

		public override object Evaluate(T json, JsonValue root)
		{
			var left = Left.Evaluate(json, root);
			var right = Right.Evaluate(json, root);
			return ValueComparer.GreaterThanEqual(left, right);
		}
		public override string ToString()
		{
			return $"{Left} >= {Right}";
		}
		public bool Equals(IsGreaterThanEqualExpression<T> other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return base.Equals(other);
		}
		public override bool Equals(object obj)
		{
			return Equals(obj as IsGreaterThanEqualExpression<T>);
		}
		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = base.GetHashCode();
				hashCode = (hashCode * 397) ^ GetType().GetHashCode();
				return hashCode;
			}
		}
	}
}