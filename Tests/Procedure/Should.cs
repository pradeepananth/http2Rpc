using System;
using FluentAssertions;
using Xunit;

namespace nRpc.Tests.Procedure
{
    public class Should
    {
        [Fact]
        public void ThrowArgumentNullException_WhenNameIsEmpty()
        {
            Action action = () => new nRpc.Procedure<object, object>(string.Empty);
            action.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void ThrowArgumentNullException_WhenNameIsNull()
        {
            Action action = () => new nRpc.Procedure<object, object>(null);
            action.Should().Throw<ArgumentNullException>();
        }
        
        [Fact]
        public void SetName()
        {
            const string Name = "anyName";
            var function = new nRpc.Procedure<object, object>(Name);
            function.Name.Should().Be(Name);            
        }
    }
}