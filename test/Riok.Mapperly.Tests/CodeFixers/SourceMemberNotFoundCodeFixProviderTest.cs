namespace Riok.Mapperly.Tests.CodeFixers;

public class SourceMemberNotFoundCodeFixProviderTest
{
    [Fact]
    public async Task X()
    {
        var source = TestSourceBuilder.Mapping("A", "B", "class A;", "class B { public int Value { get; set; } }");
        var fixedCode = await TestCodeFixHelper.Fix<SourceMemberNotFoundCodeFixProvider>(source);
        await Verify(fixedCode);
    }
}
