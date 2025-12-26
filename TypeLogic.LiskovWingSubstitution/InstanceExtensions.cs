using System;

namespace TypeLogic.LiskovWingSubstitution
{
    public static class InstanceExtensions
    {
        /// <summary>
        /// Determines whether the compile-time type of <typeparamref name="T"/> can be considered
        /// a subtype of <typeparamref name="TOut"/> according to the library's variance rules.
        /// This checks type relationships at the type level (it does not inspect the runtime instance).
        /// </summary>
        /// <typeparam name="T">The source (compile-time) type.</typeparam>
        /// <typeparam name="TOut">The target type to check against.</typeparam>
        /// <param name="instance">The instance whose compile-time type will be checked.</param>
        /// <returns>True when <typeparamref name="T"/> is a subtype of <typeparamref name="TOut"/>, otherwise false.</returns>
        public static bool IsInstanceOf<T, TOut>(this T instance)
        {
            return typeof(T).IsSubtypeOf(typeof(TOut));
        }

        /// <summary>
        /// Determines whether the compile-time type of <typeparamref name="T"/> can be considered
        /// a subtype of <paramref name="targetType"/> according to the library's variance rules.
        /// </summary>
        /// <typeparam name="T">The source (compile-time) type.</typeparam>
        /// <param name="instance">The instance whose compile-time type will be checked.</param>
        /// <param name="targetType">The target <see cref="Type"/> to check against.</param>
        /// <returns>True when <typeparamref name="T"/> is a subtype of <paramref name="targetType"/>, otherwise false.</returns>
        public static bool IsInstanceOf<T>(this T instance, Type targetType)
        {
            return typeof(T).IsSubtypeOf(targetType);
        }

        /// <summary>
        /// Determines whether the compile-time type of <typeparamref name="T"/> can be considered
        /// a subtype of <paramref name="targetType"/> and returns the runtime substitution type when found.
        /// </summary>
        /// <typeparam name="T">The source (compile-time) type.</typeparam>
        /// <param name="instance">The instance whose compile-time type will be checked.</param>
        /// <param name="targetType">The target <see cref="Type"/> to check against.</param>
        /// <param name="runtimeType">When the method returns, contains the concrete type that can be used as a substitute for <paramref name="targetType"/>, or null when no substitution is possible.</param>
        /// <returns>True when a valid substitution type is found; otherwise false.</returns>
        public static bool IsInstanceOf<T>(this T instance, Type targetType, out Type runtimeType)
        {
            return typeof(T).IsSubtypeOf(targetType, out runtimeType);
        }
    }
}
