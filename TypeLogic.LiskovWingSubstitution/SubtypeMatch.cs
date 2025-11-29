using System;

namespace TypeLogic.LiskovWingSubstitutions
{
    internal struct SubtypeMatch : System.IEquatable<SubtypeMatch>
    {
        public Type Source { get; }
        public Type Target { get; }

        public SubtypeMatch(Type source, Type target)
        {
            Source = source;
            Target = target;
        }

        public bool Equals(SubtypeMatch other) => Source == other.Source && Target == other.Target;
        public override bool Equals(object obj) => obj is SubtypeMatch other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source?.GetHashCode() ?? 0) * 397) ^ (Target?.GetHashCode() ?? 0);
            }
        }
    }
}
