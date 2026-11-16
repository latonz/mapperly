using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Abstractions.ReferenceHandling;

namespace Riok.Mapperly.IntegrationTests.Mapper
{
    [Mapper]
    public partial class GenericUserMappingDispatchMapper
    {
        public int CreateCalls { get; private set; }

        public int UpdateCalls { get; private set; }

        [MapDerivedType(typeof(IContainer<short>), typeof(IContainer<decimal>))]
        public partial TTarget Map<TSource, TTarget>(TSource source);

        [MapDerivedType(typeof(IContainer<short>), typeof(IContainer<decimal>))]
        public partial void Update<TSource, TTarget>(TSource source, TTarget target);

        public partial IContainer<long> MapIntContainer(IContainer<int> source);

        [UserMapping(Default = false)]
        public partial IContainer<long>? MapNullableIntContainer(IContainer<int>? source);

        [UserMapping(Default = false)]
        public partial Container<long> MapConcreteContainer(IContainer<int> source);

        public partial void UpdateIntContainer(IContainer<int> source, IContainer<long> target);

        public partial NewTarget MapNested(Source source);

        public partial void UpdateNested(Source source, ExistingTarget target);

        private partial long MapInt(int source);

        private partial double MapFloat(float source);

        private partial decimal MapShort(short source);

        private IContainer<TTarget> CreateContainer<TSource, TTarget>(IContainer<TSource> source)
        {
            CreateCalls++;
            return new Container<TTarget>(Map<TSource, TTarget>(source.Value));
        }

        private void UpdateContainer<TSource, TTarget>(IContainer<TSource> source, IContainer<TTarget> target)
        {
            UpdateCalls++;
            target.Value = Map<TSource, TTarget>(source.Value);
        }

        public interface IContainer<T>
        {
            T Value { get; set; }
        }

        public class Container<T> : IContainer<T>
        {
            public Container(T value) => Value = value;

            public T Value { get; set; }
        }

        public class Source
        {
            public IContainer<float> Value { get; set; } = new Container<float>(0);
        }

        public class NewTarget
        {
            public IContainer<double> Value { get; set; } = new Container<double>(0);
        }

        public class ExistingTarget
        {
            public IContainer<double> Value { get; } = new Container<double>(0);
        }
    }

    [Mapper(UseReferenceHandling = true)]
    public partial class GenericUserMappingReferenceHandlerMapper
    {
        public IReferenceHandler? LastHandler { get; private set; }

        public partial GenericUserMappingDispatchMapper.IContainer<long> Map(
            GenericUserMappingDispatchMapper.IContainer<int> source,
            [ReferenceHandler] IReferenceHandler handler
        );

        public partial void Update(
            GenericUserMappingDispatchMapper.IContainer<int> source,
            GenericUserMappingDispatchMapper.IContainer<long> target,
            [ReferenceHandler] IReferenceHandler handler
        );

        private GenericUserMappingDispatchMapper.IContainer<long> Create<T>(
            GenericUserMappingDispatchMapper.IContainer<T> source,
            [ReferenceHandler] IReferenceHandler handler
        )
        {
            LastHandler = handler;
            return new GenericUserMappingDispatchMapper.Container<long>(123);
        }

        private void UpdateContainer<T>(
            GenericUserMappingDispatchMapper.IContainer<T> source,
            GenericUserMappingDispatchMapper.IContainer<long> target,
            [ReferenceHandler] IReferenceHandler handler
        )
        {
            LastHandler = handler;
            target.Value = 123;
        }
    }
}
