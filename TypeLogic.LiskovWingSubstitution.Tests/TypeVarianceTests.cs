using System;
using System.Collections.Generic;
using TypeLogic.LiskovWingSubstitution.Tests.TestTypes;
using Xunit;

namespace TypeLogic.LiskovWingSubstitution.Tests
{
    /// <summary>
    /// Description résumée pour UnitTest1
    /// </summary>
    public class TypeVarianceTests
    {
        [Fact]
        public void Tests_00_NativeVarianceTests()
        {
            Type runtimeType = null;

            Assert.True(typeof(Object).IsSubtypeOf(typeof(Object), out runtimeType));
            Assert.Equal(typeof(Object), runtimeType);

            Assert.True(typeof(EntityType).IsSubtypeOf(typeof(IEntityType), out runtimeType));
            Assert.Equal(typeof(EntityType), runtimeType);

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
        public void Tests_01_Liskov_GenericTypesVariance()
        {
            Type runtimeType = null;

            Assert.True(typeof(List<EntityType>).IsSubtypeOf(typeof(ICollection<IEntityType>), out runtimeType));
            Assert.Equal(typeof(ICollection<EntityType>), runtimeType);

            Assert.True(typeof(List<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<EntityType>>), out runtimeType));
            Assert.True(typeof(List<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<IEntityType>>), out runtimeType));

            Assert.True(typeof(ICollection<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<EntityType>>), out runtimeType));
            Assert.True(typeof(ICollection<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<IEntityType>>), out runtimeType));
        }

        [Fact]
        public void Tests_02_Liskov_GenericTypeDefinitionsVarianceTests()
        {
            Type runtimeType = null;

            Assert.True(typeof(DateTimeRange).IsSubtypeOf(typeof(Range<>), out runtimeType));
            Assert.Equal(typeof(Range<DateTime>), runtimeType);

            Assert.True(typeof(List<EntityType>).IsSubtypeOf(typeof(ICollection<>), out runtimeType));
            Assert.Equal(typeof(ICollection<EntityType>), runtimeType);

            Assert.True(typeof(List<EntityType>).IsSubtypeOf(typeof(ICollection<IEntityType>), out runtimeType));
            Assert.Equal(typeof(ICollection<EntityType>), runtimeType);

            Assert.True(typeof(List<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<EntityType>>), out runtimeType));
            Assert.True(typeof(List<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<IEntityType>>), out runtimeType));

            Assert.True(typeof(ICollection<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<EntityType>>), out runtimeType));

            //Note in this case wouldn't it be more useful to return a runtime type of ICollection<IGenericEntityType<EntityType>> ?
            Assert.True(typeof(ICollection<SpecificEntityType>).IsSubtypeOf(typeof(ICollection<IGenericEntityType<IEntityType>>), out runtimeType));

            //Note in this case wouldn't it be more useful to return a runtime type of IComparable<Range<DateTime>> ?
            Assert.True(typeof(DateTimeRange).IsSubtypeOf(typeof(IComparable<IRange>), out runtimeType));
            Assert.Equal(typeof(IComparable<Range<DateTime>>), runtimeType);
        }
    }
}
