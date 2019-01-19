using System;
using System.Collections.Generic;
using FluentAssertions;
using nRpc.Server;
using nRpc.Utils;
using nRpc.Tests.Data;
using Xunit;

namespace nRpc.Tests.Server
{
    // TODO: Invalid NRPC function tests
    public class InvokerTest
    {
        private static readonly ExampleClass _exampleInstance = new ExampleClass();

        [Theory]
        [MemberData(nameof(Data))]
        public void Bind_WhenGenericIsNotInterface_ShouldThrowArgumentException(Invoker invoker)
        {
            Action action = () => invoker.Bind<object>(new object());
            action.Should().Throw<ArgumentException>();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Bind_WhenProvidedInstanceIsNull_ShouldThrowArgumentNullException(Invoker invoker)
        {
            Action action = () => invoker.Bind<ExampleInterface>(null);
            action.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void ShouldBindSucessfully(Invoker invoker)
        {
            Action action = () => invoker.Bind<ExampleInterface>(_exampleInstance);
            action.Should().NotThrow();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Invoke_WhenNoBinding_ShouldThrowException(Invoker invoker)
        {
            Action action = () => invoker.Invoke(typeof(NotImplementedInterface).GetName(), nameof(ExampleInterface.DoSomething));
            action.Should().Throw<Exception>();
        }

        [Theory]
        [MemberData(nameof(Data))]
        public void Invoke_WhenBindingExists_ShouldInvokeWithReturnValue(Invoker invoker)
        {
            invoker.Bind<ExampleInterface>(_exampleInstance);
            
            var returnVal = invoker.Invoke(typeof(ExampleInterface).GetName(), nameof(ExampleInterface.DoSomething));
            
            returnVal.Should().BeOfType(typeof(ExampleMessage));
            var converted = returnVal as ExampleMessage;
            converted.ExampleField.Should().Be(1);
        }

        public static IEnumerable<object[]> Data => new List<object[]>
        {
            new object[] { new Invoker() }
        };

        private interface NotImplementedInterface {}
    }
}