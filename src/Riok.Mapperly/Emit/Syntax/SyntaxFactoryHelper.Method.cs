using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Riok.Mapperly.Emit.Syntax;

public partial struct SyntaxFactoryHelper
{
    public static MethodDeclarationSyntax PublicStaticExternMethod(
        SourceEmitterContext ctx,
        TypeSyntax returnType,
        string methodName,
        ParameterListSyntax parameterList,
        SyntaxList<AttributeListSyntax> attributes
    )
    {
        return MethodDeclaration(returnType, Identifier(methodName))
            .WithModifiers(
                TokenList(
                    TrailingSpacedToken(SyntaxKind.PublicKeyword),
                    TrailingSpacedToken(SyntaxKind.StaticKeyword),
                    TrailingSpacedToken(SyntaxKind.ExternKeyword)
                )
            )
            .WithParameterList(parameterList)
            .WithAttributeLists([ctx.SyntaxFactory.GeneratedCodeAttribute(), .. attributes])
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }
}
