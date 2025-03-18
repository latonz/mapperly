using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Riok.Mapperly.Common.Syntax;

public partial struct SyntaxFactoryHelper
{
    private const string MapperIgnoreSourceAttributeName = "Riok.Mapperly.Abstractions.MapperIgnoreSource";

    public AttributeListSyntax MapperIgnoreSourceAttribute(string member)
    {
        return Attribute(MapperIgnoreSourceAttributeName, ParameterNameOfOrStringLiteral(IdentifierName(member)));
    }
}
