using System;
using System.Collections.Generic;
using TypeLogic.LiskovWingSubstitutions.Tests.TestTypes;
using Xunit;

namespace TypeLogic.LiskovWingSubstitutions.Tests
{
    /// <summary>
    /// Contains unit tests that verify Liskow-Wing substitutability.
    /// </summary>
    public class TypeVarianceTests
    {
        [Fact]
        /// <summary>
        /// Trivial checks for direct subtyping relationships that should be satisfied by native .NET Type.IsAssignableFrom method
        /// </summary>
        public void Tests_00_Direct_Subtyping_Checks()
        {
            Type runtimeType = null;

            Assert.True(typeof(Object).IsSubtypeOf(typeof(Object), out runtimeType));
            Assert.Equal(typeof(Object), runtimeType);

            Assert.True(typeof(EntityType).IsSubtypeOf(typeof(IEntityType), out runtimeType));
            Assert.Equal(typeof(IEntityType), runtimeType);

            Assert.True(typeof(Range<DateTime>).IsSubtypeOf(typeof(IComparable<Range<DateTime>>), out runtimeType));
            Assert.Equal(typeof(IComparable<Range<DateTime>>), runtimeType);

            Assert.True(typeof(DateTimeRange).IsSubtypeOf(typeof(Range<DateTime>), out runtimeType));
            Assert.Equal(typeof(Range<DateTime>), runtimeType);

            Assert.True(typeof(DateTimeRange).IsSubtypeOf(typeof(IComparable<DateTimeRange>), out runtimeType));
            Assert.Equal(typeof(IComparable<DateTimeRange>), runtimeType);

            Assert.True(typeof(DateTimeRange).IsSubtypeOf(typeof(IComparable<Range<DateTime>>), out runtimeType));
            Assert.Equal(typeof(IComparable<Range<DateTime>>), runtimeType);

            Assert.True(typeof(List<EntityType>).IsSubtypeOf(typeof(ICollection<EntityType>), out runtimeType));

            Assert.True(typeof(List<int>).IsSubtypeOf(typeof(IEnumerable<int>), out runtimeType));
            Assert.Equal(typeof(IEnumerable<int>), runtimeType);

            Assert.False(typeof(ICollection<Exception>).IsSubtypeOf(typeof(ICollection<IEntityType>), out runtimeType));
        }

        [Fact]
        public void Tests_01_Liskov_Subtyping_GenericTypes_Checks()
        {
            Type runtimeType = null;

            Assert.True(typeof(List<EntityType>).IsSubtypeOf(typeof(ICollection<IEntityType>), out runtimeType));
            Assert.Equal(typeof(ICollection<IEntityType>), runtimeType);

            Assert.True(typeof(List<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<EntityType>>), out runtimeType));
            Assert.True(typeof(List<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<IEntityType>>), out runtimeType));

            Assert.True(typeof(ICollection<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<EntityType>>), out runtimeType));

            Assert.True(typeof(ICollection<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<EntityType>>), out runtimeType));

            //Note in this case wouldn't it be more useful to return a runtime type of ICollection<IGenericEntityType<EntityType>> ?
            Assert.True(typeof(ICollection<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<IEntityType>>), out runtimeType));

            //Note in this case wouldn't it be more useful to return a runtime type of IComparable<Range<DateTime>> ?
            Assert.True(typeof(DateTimeRange).IsSubtypeOf(typeof(IComparable<IRange>), out runtimeType));
            Assert.Equal(typeof(IComparable<IRange>), runtimeType);
        }

        [Fact]
        public void Tests_02_Liskov_Subtyping_GenericTypeDefinitions_Checks()
        {
            Type runtimeType = null;

            Assert.True(typeof(DateTimeRange).IsSubtypeOf(typeof(Range<>), out runtimeType));
            Assert.Equal(typeof(Range<DateTime>), runtimeType);

            Assert.True(typeof(List<EntityType>).IsSubtypeOf(typeof(ICollection<>), out runtimeType));
            Assert.Equal(typeof(ICollection<EntityType>), runtimeType);
        }
    }
}