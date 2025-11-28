using System;
using System.Linq.Expressions;

namespace TypeLogic.LiskovWingSubstitutions
{
    /// <summary>
    /// Holds information about a discovered conversion between a source type and a runtime type used for substitution.
    /// </summary>
    internal sealed class ConversionInfo
    {
        /// <summary>
        /// Builds a lazy delegate factory for converting from the source type to the runtime type.
        /// </summary>
        private static readonly Func<VariantTypePair, Type, Lazy<Delegate>> BuildDelegate =
            (t, rt) => new Lazy<Delegate>(() => CreateDelegate(t.Source, rt));

        /// <summary>
        /// Gets the resolved runtime type that can be used to satisfy the expected type.
        /// </summary>
        public Type RuntimeType { get; }

        /// <summary>
        /// Gets a lazy-initialized converter delegate that converts instances from the source type to <see cref="RuntimeType"/>
        /// </summary>
        public Lazy<Delegate> Converter { get; }

        /// <summary>
        /// A sentinel <see cref="ConversionInfo"/> representing a negative (non-convertible) result.
        /// </summary>
        public static readonly ConversionInfo Negative = new ConversionInfo(null, null);

        private ConversionInfo(Type runtimeType, Lazy<Delegate> converter)
        {
            RuntimeType = runtimeType;
            Converter = converter;
        }

        /// <summary>
        /// Gets a value indicating whether the conversion is possible.
        /// </summary>
        public bool IsConvertible => RuntimeType != null;

        /// <summary>
        /// Creates a <see cref="ConversionInfo"/> for the specified type pair and runtime type.
        /// If the provided runtime type equals the source type but the target is a generic definition,
        /// this method will attempt to resolve the appropriate constructed base generic type (e.g. Range<DateTime>).
        /// </summary>
        /// <param name="typePair">The source/target type pair used as cache key.</param>
        /// <param name="runtimeType">The resolved runtime type to use for conversion.</param>
        /// <returns>A new <see cref="ConversionInfo"/> instance containing the runtime type and converter factory.</returns>
        public static ConversionInfo Register(VariantTypePair typePair, Type runtimeType)
        {
            // If the runtimeType provided is the same as the source, and the target is a generic type definition,
            // try to locate a constructed base type on the source that matches that generic definition.
            if (runtimeType == typePair.Source && typePair.Target != null && typePair.Target.IsGenericType && typePair.Target.IsGenericTypeDefinition)
            {
                var cur = typePair.Source.BaseType;
                while (cur != null)
                {
                    if (cur.IsGenericType && cur.GetGenericTypeDefinition() == typePair.Target)
                    {
                        runtimeType = cur; // e.g. Range<DateTime>
                        break;
                    }
                    cur = cur.BaseType;
                }
            }

            return new ConversionInfo(runtimeType, BuildDelegate(typePair, runtimeType));
        }

        /// <summary>
        /// Creates a delegate that performs a runtime conversion from <paramref name="sourceType"/> to <paramref name="runtimeType"/>.
        /// </summary>
        /// <param name="sourceType">The compile-time source type.</param>
        /// <param name="runtimeType">The runtime type to convert to.</param>
        /// <returns>A compiled delegate that converts instances of <paramref name="sourceType"/> to <paramref name="runtimeType"/>.</returns>
        private static Delegate CreateDelegate(Type sourceType, Type runtimeType)
        {
            var lbdParam = Expression.Parameter(sourceType);
            return Expression.Lambda(Expression.Convert(lbdParam, runtimeType), lbdParam).Compile();
        }
    }
}