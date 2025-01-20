using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Configuration;
using Riok.Mapperly.Emit.Syntax;

namespace Riok.Mapperly.Descriptors.Mappings.DerivedTypes;

/// <summary>
/// A derived type mapping maps one base type or interface to another
/// by implementing a if with instance checks over known types and performs the provided mapping for each type.
/// </summary>
public class DerivedTypeIfExpressionMapping(
    ITypeSymbol sourceType,
    ITypeSymbol targetType,
    IReadOnlyCollection<DerivedTypeMapping> derivedTypeMappings
) : NewInstanceMapping(sourceType, targetType)
{
    public override ExpressionSyntax Build(TypeMappingBuildContext ctx)
    {
        // source is A x ? MapToA(x) : <other cases>
        var typeExpressions = derivedTypeMappings
            .Reverse()
            .Aggregate<DerivedTypeMapping, ExpressionSyntax>(
                SyntaxFactoryHelper.DefaultLiteral(),
                (aggregate, current) => BuildConditional(ctx, aggregate, current)
            );

        // cast to target type, to ensure the compiler picks the correct type
        // (B)(<ifs...>
        return SyntaxFactory.CastExpression(
            SyntaxFactoryHelper.FullyQualifiedIdentifier(TargetType),
            SyntaxFactory.ParenthesizedExpression(typeExpressions)
        );
    }

    private ConditionalExpressionSyntax BuildConditional(
        TypeMappingBuildContext ctx,
        ExpressionSyntax notMatched,
        DerivedTypeMapping mapping
    )
    {
        // cannot use is pattern matching is operator due to expression limitations
        // use is with a cast instead
        // source is A ? MapToB((A)x) : <other cases>
        var castedSourceContext = ctx.WithSource(
            SyntaxFactory.ParenthesizedExpression(
                SyntaxFactory.CastExpression(SyntaxFactoryHelper.FullyQualifiedIdentifier(mapping.SourceType), ctx.Source)
            )
        );
        var condition = SyntaxFactoryHelper.Is(ctx.Source, SyntaxFactoryHelper.FullyQualifiedIdentifier(mapping.SourceType));
        return SyntaxFactoryHelper.Conditional(condition, mapping.Build(castedSourceContext), notMatched);
    }
}
