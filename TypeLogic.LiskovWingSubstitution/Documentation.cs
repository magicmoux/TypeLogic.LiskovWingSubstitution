using System;

namespace TypeLogic.LiskovWingSubstitutions
{
    /// <summary>
    /// General documentation for the TypeLogic.LiskovWingSubstitution assembly.
    /// </summary>
    /// <remarks>
    /// This assembly implements utilities to determine whether a runtime type can be used
    /// in place of another type according to the Liskov/Wing Substitution Principle.
    ///
    /// IMPORTANT: throughout this assembly the term "subtype" (and related terms like
    /// "IsSubtypeOf", "IsSubtypeOf", or "IsInstanceOf") refers to the stronger,
    /// behavioral Liskov/Wing definition of subtyping — not merely a syntactic or
    /// structural subtyping relation (for example, name equality, matching shapes,
    /// or assignability alone).
    ///
    /// In practice this means conversions validated by these helpers consider the
    /// semantics of generic variance, generic parameter constraints and behavioral
    /// substitutability (Liskov/Wing) rather than only simple type identity or
    /// superficial structural compatibility.
    /// </remarks>
    public static class AssemblyDocumentation
    {
        /// <summary>
        /// Marker type used only to carry assembly-level XML documentation.
        /// </summary>
        /// <remarks>
        /// This type is intentionally empty. Its XML comments are used to document
        /// the assembly-level meaning of "subtype" and related API semantics.
        /// </remarks>
        public static readonly Type Marker = typeof(AssemblyDocumentation);
    }
}
