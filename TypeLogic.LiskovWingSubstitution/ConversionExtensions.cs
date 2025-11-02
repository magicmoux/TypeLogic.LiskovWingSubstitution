using System;
using System.Linq;
using System.Reflection;

namespace TypeLogic.LiskovWingSubstitutions
{
    public static partial class ConversionExtensions
    {
        private static readonly Lazy<MethodInfo> LazyConvertAsDefinition = new Lazy<MethodInfo>(() => 
            typeof(ConversionExtensions).GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .Where(m => m.Name == "ConvertAs" && m.GetGenericArguments().Count() == 2)
                .FirstOrDefault()?.GetGenericMethodDefinition());

        private static MethodInfo ConvertAsDefinition => LazyConvertAsDefinition.Value;

        public static dynamic ConvertAs<T>(this T instance, Type expectedType)
        {
            // Handle null instance
            if (instance == null)
            {
                return null;
            }

            Type actualType = instance.GetType();
            Type targetType = expectedType;

            // If expected type is a generic type definition, try to construct a closed type
            if (expectedType.IsGenericTypeDefinition)
            {
                // For string to IEnumerable<char> conversion
                if (actualType == typeof(string) && expectedType == typeof(System.Collections.Generic.IEnumerable<>))
                {
                    targetType = typeof(System.Collections.Generic.IEnumerable<char>);
                }
                else
                {
                    // Try to construct the generic type with the type arguments from the actual type
                    Type[] typeArgs = actualType.GetGenericArguments();
                    if (typeArgs.Length > 0)
                    {
                        try
                        {
                            targetType = expectedType.MakeGenericType(typeArgs);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                }
            }

            // Check if conversion is possible and get the runtime type
            if (actualType.IsVariantOf(targetType, out var runtimeType))
            {
                try
                {
                    var method = ConvertAsDefinition?.MakeGenericMethod(typeof(T), runtimeType);
                    if (method != null)
                    {
                        return method.Invoke(null, new object[] { instance, targetType });
                    }
                }
                catch (Exception)
                {
                    // If conversion fails, return null instead of throwing
                    return null;
                }
            }
            return null;
        }

        private static TOut ConvertAs<T, TOut>(this T instance, Type expectedType)
        {
            var key = new VariantTypePair(typeof(T), expectedType);
            if (TypeExtensions._conversionCache.TryGetValue(key, out var conversion) && conversion.IsConvertible && conversion.Converter != null)
            {
                try
                {
                    return (TOut)conversion.Converter.Value.DynamicInvoke(instance);
                }
                catch
                {
                    return default(TOut);
                }
            }
            return default(TOut);
        }

        public static bool IsInstanceOf<T, TOut>(this T instance)
        {
            if (instance == null) return typeof(T).IsVariantOf(typeof(TOut));
            return instance.GetType().IsVariantOf(typeof(TOut));
        }

        public static bool IsInstanceOf<T>(this T instance, Type targetType)
        {
            if (instance == null) return typeof(T).IsVariantOf(targetType);
            return instance.GetType().IsVariantOf(targetType);
        }

        public static bool IsInstanceOf<T>(this T instance, Type targetType, out Type runtimeType)
        {
            if (instance == null) return typeof(T).IsVariantOf(targetType, out runtimeType);
            return instance.GetType().IsVariantOf(targetType, out runtimeType);
        }
    }
}