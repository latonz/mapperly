using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Emit.Syntax;
using Riok.Mapperly.Symbols.Members;

namespace Riok.Mapperly.Descriptors.Mappings.DerivedTypes;

/// <summary>
/// A derived type mapping maps one base type or interface to another
/// by implementing a type switch over known types and performs the provided mapping for each type.
/// </summary>
public class DerivedTypeSwitchMapping(
    ITypeSymbol sourceType,
    ITypeSymbol targetType,
    MemberPathGetter? discriminatorGetter,
    IReadOnlyCollection<DerivedTypeMapping> derivedMappings
) : NewInstanceMapping(sourceType, targetType)
{
    private const string GetTypeMethodName = nameof(GetType);

    public override ExpressionSyntax Build(TypeMappingBuildContext ctx)
    {
        // _ => throw new ArgumentException(msg, nameof(ctx.Source)),
        var sourceTypeExpr = ctx.SyntaxFactory.Invocation(SyntaxFactoryHelper.MemberAccess(ctx.Source, GetTypeMethodName));
        var fallbackArm = SyntaxFactoryHelper.SwitchArm(
            SyntaxFactory.DiscardPattern(),
            SyntaxFactoryHelper.ThrowArgumentExpression(
                SyntaxFactoryHelper.InterpolatedString(
                    $"Cannot map {sourceTypeExpr} to {TargetType.ToDisplayString()} as there is no known derived type mapping"
                ),
                ctx.Source
            )
        );

        // TODO null handling
        var discriminatorValue = discriminatorGetter?.BuildAccess(ctx.Source);

        // source switch { A x => MapToADto(x), B x => MapToBDto(x) }
        var arms = derivedMappings.Select(x => x.BuildSwitchArm(ctx, discriminatorValue)).Append(fallbackArm);
        return ctx.SyntaxFactory.Switch(ctx.Source, arms);
    }
}
