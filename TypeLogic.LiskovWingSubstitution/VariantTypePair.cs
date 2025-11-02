using System;

namespace TypeLogic.LiskovWingSubstitutions
{
    internal struct VariantTypePair : System.IEquatable<VariantTypePair>
    {
        public Type Source { get; }
        public Type Target { get; }

        public VariantTypePair(Type source, Type target)
        {
            Source = source;
            Target = target;
        }

        public bool Equals(VariantTypePair other) => Source == other.Source && Target == other.Target;
        public override bool Equals(object obj) => obj is VariantTypePair other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Source?.GetHashCode() ?? 0) * 397) ^ (Target?.GetHashCode() ?? 0);
            }
        }
    }
}