using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Abstractions;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Riok.Mapperly.Emit.Syntax;

public partial struct SyntaxFactoryHelper
{
    private static readonly string MapperIgnoreSourceAttributeName = typeof(MapperIgnoreSourceAttribute).FullName!;

    public AttributeListSyntax MapperIgnoreSourceAttribute(string member)
    {
        return Attribute(MapperIgnoreSourceAttributeName, ParameterNameOfOrStringLiteral(IdentifierName(member)));
    }
}
