using Riok.Mapperly.Diagnostics;

namespace Riok.Mapperly.Tests.Mapping;

public class GenericUserMappingTest
{
    private const string Container = "public interface IContainer<T> { T Value { get; set; } }";
    private const string NewInstanceHelper =
        "private IContainer<TTarget> Convert<TSource, TTarget>(IContainer<TSource> source) => throw new NotImplementedException();";
    private const string ExistingTargetHelper =
        "private void Update<TSource, TTarget>(IContainer<TSource> source, IContainer<TTarget> target) => throw new NotImplementedException();";

    [Fact]
    public void ConcretePartialShouldDelegateToGenericHelper()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "public partial IContainer<long> Map(IContainer<int> source);" + NewInstanceHelper,
            Container
        );

        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return Convert<int, long>(source);");
    }

    [Fact]
    public void ConcreteExistingTargetPartialShouldDelegateToGenericHelper()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "public partial void Map(IContainer<int> source, IContainer<long> target);" + ExistingTargetHelper,
            Container
        );

        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("Update<int, long>(source, target);");
    }

    [Fact]
    public void NullableConcretePartialShouldGuardNonNullableHelper()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "public partial IContainer<long>? Map(IContainer<int>? source);" + NewInstanceHelper,
            Container
        );

        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return source == null ? default : Convert<int, long>(source);");
    }

    [Fact]
    public void NullableConcretePartialShouldPassNullToNullableHelper()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            public partial IContainer<long>? Map(IContainer<int>? source);
            private IContainer<TTarget> Convert<TSource, TTarget>(IContainer<TSource>? source) => throw new NotImplementedException();
            """,
            Container
        );

        TestHelper.GenerateMapper(source).Should().HaveSingleMethodBody("return Convert<int, long>(source);");
    }

    [Fact]
    public void NullableExistingTargetPartialShouldGuardNonNullableHelper()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "public partial void Map(IContainer<int>? source, IContainer<long>? target);" + ExistingTargetHelper,
            Container
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                if (source == null || target == null)
                    return;
                Update<int, long>(source, target);
                """
            );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DispatchShouldIncludeConcretePartialRegardlessOfDeclarationOrder(bool dispatcherFirst)
    {
        const string dispatcher = "public partial TTarget Dispatch<TSource, TTarget>(TSource source);";
        const string mapping = "public partial IContainer<long> Map(IContainer<int> source);";
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            (dispatcherFirst ? dispatcher + mapping : mapping + dispatcher) + NewInstanceHelper,
            Container
        );

        var result = TestHelper.GenerateMapper(source);
        result.Should().HaveMapMethodBody("return Convert<int, long>(source);");
        result.Methods["Dispatch"].Body.ShouldContain("global::IContainer<int> x");
        result.Methods["Dispatch"].Body.ShouldContain("(TTarget)(object)Map(x)");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingTargetDispatchShouldIncludeConcretePartialRegardlessOfDeclarationOrder(bool dispatcherFirst)
    {
        const string dispatcher = "public partial void Dispatch<TSource, TTarget>(TSource source, TTarget target);";
        const string mapping = "public partial void Map(IContainer<int> source, IContainer<long> target);";
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            (dispatcherFirst ? dispatcher + mapping : mapping + dispatcher) + ExistingTargetHelper,
            Container
        );

        var result = TestHelper.GenerateMapper(source);
        result.Should().HaveMapMethodBody("Update<int, long>(source, target);");
        result.Methods["Dispatch"].Body.ShouldContain("case (global::IContainer<int>");
        result.Methods["Dispatch"].Body.ShouldContain("Map(source1, target1);");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DispatchShouldIncludeNestedSpecializationsRegardlessOfDeclarationOrder(bool dispatcherFirst)
    {
        const string dispatchers = """
            public partial TTarget Dispatch<TSource, TTarget>(TSource source);
            public partial object DispatchRuntime(object source, Type targetType);
            """;
        const string mapping = "public partial B Map(A source);";
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            (dispatcherFirst ? dispatchers + mapping : mapping + dispatchers) + NewInstanceHelper,
            Container,
            "class A { public IContainer<int> First { get; set; } public IContainer<float> Second { get; set; } }",
            "class B { public IContainer<long> First { get; set; } public IContainer<double> Second { get; set; } }"
        );

        var result = TestHelper.GenerateMapper(source);
        foreach (var name in new[] { "Dispatch", "DispatchRuntime" })
        {
            result.Methods[name].Body.ShouldContain("Convert<int, long>(x)");
            result.Methods[name].Body.ShouldContain("Convert<float, double>(x)");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExistingTargetDispatchShouldIncludeNestedSpecializationsRegardlessOfDeclarationOrder(bool dispatcherFirst)
    {
        const string dispatcher = "public partial void Dispatch<TSource, TTarget>(TSource source, TTarget target);";
        const string mapping = "public partial void Map(A source, B target);";
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            (dispatcherFirst ? dispatcher + mapping : mapping + dispatcher) + ExistingTargetHelper,
            Container,
            "class A { public IContainer<int> First { get; set; } public IContainer<float> Second { get; set; } }",
            "class B { public IContainer<long> First { get; } public IContainer<double> Second { get; } }"
        );

        var result = TestHelper.GenerateMapper(source);
        result.Methods["Dispatch"].Body.ShouldContain("Update<int, long>(source1, target1);");
        result.Methods["Dispatch"].Body.ShouldContain("Update<float, double>(source1, target1);");
    }

    [Fact]
    public void DispatchShouldIncludeNullableParentConverterSpecialization()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            public partial TTarget Dispatch<TSource, TTarget>(TSource source);
            [MapProperty("Item.Value", "Value")]
            public partial Target Map(Source source);
            private T Convert<T>(T? source) where T : struct => source ?? default;
            """,
            "record Source(SourceItem? Item);",
            "record struct SourceItem(int Value);",
            "record Target(int Value);"
        );

        var result = TestHelper.GenerateMapper(source);
        result.Methods["Map"].Body.ShouldContain("Convert<int>(source.Item?.Value)");
        result.Methods["Dispatch"].Body.ShouldContain("Convert<int>(x)");
    }

    [Fact]
    public void ExistingTargetDispatchWithOnlyOpenTemplateShouldDiagnosticWithoutCrashing()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "public partial void Map<TSource, TTarget>(TSource source, TTarget target);" + ExistingTargetHelper,
            Container
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(DiagnosticDescriptors.RuntimeTargetTypeMappingNoContentMappings)
            .HaveAssertedAllDiagnostics();
    }

    [Fact]
    public void ExistingTargetDispatchShouldResolveExplicitDerivedPairs()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            [MapDerivedType<IContainer<int>, IContainer<long>>]
            [MapDerivedType<IContainer<int>, IContainer<double>>]
            public partial void Map<TSource, TTarget>(TSource source, TTarget target);
            """ + ExistingTargetHelper,
            Container
        );

        var result = TestHelper.GenerateMapper(source);
        result.Methods["Map"].Body.ShouldContain("Update<int, long>(source1, target1);");
        result.Methods["Map"].Body.ShouldContain("Update<int, double>(source1, target1);");
    }

    [Fact]
    public void DispatchShouldSeeSpecializationsDiscoveredByAnotherDispatcher()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            public partial TTarget Map<TSource, TTarget>(TSource source);
            [MapDerivedType<A, B>]
            public partial TTarget Other<TSource, TTarget>(TSource source);
            """ + NewInstanceHelper,
            Container,
            "class A { public IContainer<int> Value { get; set; } }",
            "class B { public IContainer<long> Value { get; set; } }"
        );

        var result = TestHelper.GenerateMapper(source);
        result.Methods["Map"].Body.ShouldContain("Convert<int, long>(x)");
        result.Methods["Other"].Body.ShouldContain("Convert<int, long>(x)");
    }

    [Fact]
    public void ConcretePartialWithIncompatibleGenericConstraintShouldDiagnostic()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            public partial IContainer<long> Map(IContainer<int> source);
            private IContainer<TTarget> Convert<TSource, TTarget>(IContainer<TSource> source)
                where TSource : class => throw new NotImplementedException();
            """,
            Container
        );

        TestHelper
            .GenerateMapper(source, TestHelperOptions.AllowDiagnostics)
            .Should()
            .HaveDiagnostic(DiagnosticDescriptors.NoConstructorFound)
            .HaveAssertedAllDiagnostics();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ExplicitMemberConfigurationShouldNotBeBypassed(bool existingTarget)
    {
        var signature = existingTarget ? "public partial void Map(A source, B target);" : "public partial B Map(A source);";
        var helper = existingTarget
            ? "private void Convert<TSource, TTarget>(TSource source, TTarget target) => throw new NotImplementedException();"
            : "private TTarget Convert<TSource, TTarget>(TSource source) => throw new NotImplementedException();";
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "[MapProperty(nameof(A.Value), nameof(B.Other), Use = nameof(MapValue))]"
                + signature
                + helper
                + "private int MapValue(int source) => source + 1;",
            "class A { public int Value { get; set; } }",
            "class B { public int Other { get; set; } }"
        );

        var result = TestHelper.GenerateMapper(source);
        result.Methods["Map"].Body.ShouldContain("target.Other = MapValue(source.Value);");
        result.Methods["Map"].Body.ShouldNotContain("Convert<");
    }

    [Fact]
    public void InterfaceReturningHelperShouldNotSatisfyConcreteClassTarget()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            "public partial Container<long> Map(IContainer<int> source);" + NewInstanceHelper,
            Container,
            "class Container<T> : IContainer<T> { public T Value { get; set; } }"
        );

        TestHelper
            .GenerateMapper(source)
            .Should()
            .HaveSingleMethodBody(
                """
                var target = new global::Container<long>();
                target.Value = (long)source.Value;
                return target;
                """
            );
    }

    [Fact]
    public void ExactNonGenericMappingShouldTakePrecedenceInNestedMappingAndDispatch()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            public partial TTarget Dispatch<TSource, TTarget>(TSource source);
            public partial B Map(A source);
            private IContainer<long> ConvertInt(IContainer<int> source) => throw new NotImplementedException();
            """ + NewInstanceHelper,
            Container,
            "class A { public IContainer<int> Value { get; set; } }",
            "class B { public IContainer<long> Value { get; set; } }"
        );

        var result = TestHelper.GenerateMapper(source);
        result.Methods["Map"].Body.ShouldContain("ConvertInt(source.Value)");
        result.Methods["Dispatch"].Body.ShouldContain("ConvertInt(x)");
        result.Methods["Dispatch"].Body.ShouldNotContain("Convert<int, long>");
    }

    [Fact]
    public void NonDefaultPartialShouldNotRegisterItsGenericDelegateForDispatch()
    {
        var source = TestSourceBuilder.MapperWithBodyAndTypes(
            """
            public partial TTarget Dispatch<TSource, TTarget>(TSource source);
            public partial long MapInt(int source);
            [UserMapping(Default = false)]
            public partial IContainer<long> Map(IContainer<int> source);
            """ + NewInstanceHelper,
            Container
        );

        var result = TestHelper.GenerateMapper(source);
        result.Should().HaveMapMethodBody("return Convert<int, long>(source);");
        result.Methods["Dispatch"].Body.ShouldNotContain("IContainer");
    }
}
