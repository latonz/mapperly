using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Riok.Mapperly.Common.Syntax;

public partial struct SyntaxFactoryHelper
{
    public static ExpressionSyntax BitwiseAnd(params ExpressionSyntax?[] values) =>
        BinaryExpression(SyntaxKind.BitwiseAndExpression, values);

    public static ExpressionSyntax BitwiseOr(IEnumerable<ExpressionSyntax?> values) =>
        BinaryExpression(SyntaxKind.BitwiseOrExpression, values);
}
