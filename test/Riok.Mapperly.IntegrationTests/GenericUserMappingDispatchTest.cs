using System;
using Riok.Mapperly.Abstractions.ReferenceHandling;
using Riok.Mapperly.IntegrationTests.Mapper;
using Shouldly;
using Xunit;
using static Riok.Mapperly.IntegrationTests.Mapper.GenericUserMappingDispatchMapper;

namespace Riok.Mapperly.IntegrationTests
{
    public class GenericUserMappingDispatchTest
    {
        [Fact]
        public void ConcreteNewInstanceMappingShouldCallGenericHelper()
        {
            var mapper = new GenericUserMappingDispatchMapper();
            var result = mapper.MapIntContainer(new Container<int>(42));

            result.Value.ShouldBe(42);
            mapper.CreateCalls.ShouldBe(1);
        }

        [Fact]
        public void NullSourceShouldNotBePassedToNonNullableHelper()
        {
            var mapper = new GenericUserMappingDispatchMapper();

            mapper.MapNullableIntContainer(null).ShouldBeNull();
            mapper.CreateCalls.ShouldBe(0);
            mapper.MapNullableIntContainer(new Container<int>(42))!.Value.ShouldBe(42);
            mapper.CreateCalls.ShouldBe(1);
        }

        [Fact]
        public void InterfaceReturningHelperShouldNotReplaceConcreteClassMapping()
        {
            var mapper = new GenericUserMappingDispatchMapper();

            mapper.MapConcreteContainer(new Container<int>(42)).Value.ShouldBe(42);
            mapper.CreateCalls.ShouldBe(0);
        }

        [Fact]
        public void ConcreteExistingTargetMappingShouldCallGenericHelper()
        {
            var mapper = new GenericUserMappingDispatchMapper();
            var target = new Container<long>(0);
            mapper.UpdateIntContainer(new Container<int>(42), target);

            target.Value.ShouldBe(42);
            mapper.UpdateCalls.ShouldBe(1);
        }

        [Fact]
        public void NewInstanceDispatchShouldCallGenericHelpersForEachConcretePair()
        {
            var mapper = new GenericUserMappingDispatchMapper();

            mapper.Map<IContainer<int>, IContainer<long>>(new Container<int>(42)).Value.ShouldBe(42);
            mapper.Map<IContainer<float>, IContainer<double>>(new Container<float>(12.5f)).Value.ShouldBe(12.5);
            mapper.Map<IContainer<short>, IContainer<decimal>>(new Container<short>(7)).Value.ShouldBe(7);
            mapper.CreateCalls.ShouldBe(3);
        }

        [Fact]
        public void ExistingTargetDispatchShouldCallGenericHelpersForEachConcretePair()
        {
            var mapper = new GenericUserMappingDispatchMapper();
            var longTarget = new Container<long>(0);
            var doubleTarget = new Container<double>(0);
            var decimalTarget = new Container<decimal>(0);

            mapper.Update<IContainer<int>, IContainer<long>>(new Container<int>(42), longTarget);
            mapper.Update<IContainer<float>, IContainer<double>>(new Container<float>(12.5f), doubleTarget);
            mapper.Update<IContainer<short>, IContainer<decimal>>(new Container<short>(7), decimalTarget);

            longTarget.Value.ShouldBe(42);
            doubleTarget.Value.ShouldBe(12.5);
            decimalTarget.Value.ShouldBe(7);
            mapper.UpdateCalls.ShouldBe(3);
        }

        [Fact]
        public void NestedMappingsShouldCallGenericHelpers()
        {
            var mapper = new GenericUserMappingDispatchMapper();
            var source = new Source { Value = new Container<float>(12.5f) };
            var target = new ExistingTarget();
            var originalContainer = target.Value;

            mapper.MapNested(source).Value.Value.ShouldBe(12.5);
            mapper.UpdateNested(source, target);

            target.Value.ShouldBeSameAs(originalContainer);
            target.Value.Value.ShouldBe(12.5);
            mapper.CreateCalls.ShouldBe(1);
            mapper.UpdateCalls.ShouldBe(1);
        }

        [Fact]
        public void DispatchShouldRejectUnregisteredTypePair()
        {
            var mapper = new GenericUserMappingDispatchMapper();
            var source = new Container<bool>(true);

            Should.Throw<ArgumentException>(() => mapper.Map<IContainer<bool>, IContainer<long>>(source));
            Should.Throw<ArgumentException>(() => mapper.Update<IContainer<bool>, IContainer<long>>(source, new Container<long>(0)));
            mapper.CreateCalls.ShouldBe(0);
            mapper.UpdateCalls.ShouldBe(0);
        }

        [Fact]
        public void NewInstanceGenericDelegateShouldReceiveReferenceHandler()
        {
            var mapper = new GenericUserMappingReferenceHandlerMapper();
            var handler = new PreserveReferenceHandler();

            mapper.Map(new Container<int>(42), handler).Value.ShouldBe(123);
            mapper.LastHandler.ShouldBeSameAs(handler);
        }

        [Fact]
        public void ExistingTargetGenericDelegateShouldReceiveReferenceHandler()
        {
            var mapper = new GenericUserMappingReferenceHandlerMapper();
            var handler = new PreserveReferenceHandler();
            var target = new Container<long>(0);

            mapper.Update(new Container<int>(42), target, handler);

            target.Value.ShouldBe(123);
            mapper.LastHandler.ShouldBeSameAs(handler);
        }
    }
}
