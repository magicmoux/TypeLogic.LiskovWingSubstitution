using System;
using System.Linq.Expressions;

namespace TypeLogic.LiskovWingSubstitutions
{
    internal sealed class ConversionInfo
    {
        private static readonly Func<VariantTypePair, Type, Lazy<Delegate>> BuildDelegate =
            (t, rt) => new Lazy<Delegate>(() => CreateDelegate(t.Source, rt));

        public Type RuntimeType { get; }
        public Lazy<Delegate> Converter { get; }
        public static readonly ConversionInfo Negative = new ConversionInfo(null, null);

        private ConversionInfo(Type runtimeType, Lazy<Delegate> converter)
        {
            RuntimeType = runtimeType;
            Converter = converter;
        }

        public bool IsConvertible => RuntimeType != null;

        public static ConversionInfo Register(VariantTypePair typePair, Type runtimeType)
        {
            return new ConversionInfo(runtimeType, BuildDelegate(typePair, runtimeType));
        }

        private static Delegate CreateDelegate(Type sourceType, Type runtimeType)
        {
            var lbdParam = Expression.Parameter(sourceType);
            return Expression.Lambda(Expression.Convert(lbdParam, runtimeType), lbdParam).Compile();
        }
    }
}