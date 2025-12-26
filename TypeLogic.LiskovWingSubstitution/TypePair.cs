using System;

namespace TypeLogic.LiskovWingSubstitution
{
    /// <summary>
    /// Represents a pair of source and target types used as a key in the runtime substitution cache.
    /// </summary>
    internal struct TypePair : System.IEquatable<TypePair>
    {
        /// <summary>
        /// The source type in the pair.
        /// </summary>
        public Type Source { get; }

        /// <summary>
        /// The target type in the pair.
        /// </summary>
        public Type Target { get; }

        private readonly int _hashCode;

        public TypePair(Type source, Type target)
        {
            Source = source;
            Target = target;
            unchecked
            {
                var h1 = Source?.GetHashCode() ?? 0;
                var h2 = Target?.GetHashCode() ?? 0;
                _hashCode = (h1 * 397) ^ h2;
            }
        }

        public bool Equals(TypePair other) => Source == other.Source && Target == other.Target;

        public override bool Equals(object obj) => obj is TypePair other && Equals(other);

        public override int GetHashCode() => _hashCode;
    }
}
