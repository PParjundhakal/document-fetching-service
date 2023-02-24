using Microservice.Business.Business;
using System;
using Xunit;

namespace Microservice.Test
{
    public class SampleUnitTests
    {

        [Fact]
        public void SampleTest()
        {
            // Arrange: setup and get ready for the test (sut = system under test)
            var sut = new Business.Business.Concrete.Business(null);

            // Act: invoke a system or method
            var sum = sut.AddNumbers(1, 5);

            // Assert: Did we get the result we expected?
            Assert.True(sum == 6);
        }
    }
}
