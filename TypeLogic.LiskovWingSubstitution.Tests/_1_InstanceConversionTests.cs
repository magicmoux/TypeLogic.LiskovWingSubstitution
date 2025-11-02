using System;
using System.Collections.Generic;
using TypeLogic.LiskovWingSubstitutions;
using Xunit;

namespace TypeLogic.LiskovWingSubstitutions.Tests
{
    /// <summary>
    /// Description résumée pour UnitTest1
    /// </summary>
    public class InstanceConversionTests
    {
        [Fact]
        public void Tests_00_NonConvertibleToDefault()
        {
            Exception instance = new Exception("This is an exception");
            var conversion = instance.ConvertAs(typeof(IEnumerable<>));
            Assert.Null(conversion);
        }

        [Fact]
        public void Tests_02_GenericTypeDefinitionConversions()
        {
            String instance = "This is a string";
            var conversion = instance.ConvertAs(typeof(IEnumerable<>));
            var outputConvertedType = conversion.GetType();
            Assert.IsAssignableFrom<IEnumerable<char>>(conversion);
        }
    }
}