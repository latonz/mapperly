using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Emit.Syntax;
using Riok.Mapperly.Symbols.Members;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Riok.Mapperly.Descriptors.Mappings.DerivedTypes;

// TODO
public class DerivedTypeMapping(
    ITypeSymbol sourceType,
    ITypeSymbol targetType,
    bool isExplicitSourceType,
    ExpressionSyntax? discriminatorValue,
    MemberPathGetter? member,
    INewInstanceMapping mapping
) : IMapping
{
    public ITypeSymbol SourceType { get; } = sourceType;

    public ITypeSymbol TargetType { get; } = targetType;

    // TODO
    public ExpressionSyntax Build(TypeMappingBuildContext ctx)
    {
        if (member != null)
        {
            ctx = ctx.WithSource(member.BuildAccess(ctx.Source));
        }

        return mapping.Build(ctx);
    }

    public SwitchExpressionArmSyntax BuildSwitchArm(TypeMappingBuildContext ctx, ExpressionSyntax? sourceDiscriminatorValue)
    {
        // no explicit source type set,
        // switch is done on the discriminator value.
        // source.Discriminator switch { ValueA => MapToA(source.ValueA), }
        if (!isExplicitSourceType)
        {
            return SyntaxFactoryHelper.SwitchArm(ConstantPattern(discriminatorValue!), Build(ctx));
        }

        // explicit source type set, the switch is done on the type (for easier casting syntax)
        // and the discriminator value is compared in the when clause.
        // source switch { A srcA when source.Discriminator == ValueA => MapToADto(srcA.ValueA), }
        var (typeArmContext, typeArmVariableName) = ctx.WithNewSource();

        var declaration = DeclarationPattern(
            SyntaxFactoryHelper.FullyQualifiedIdentifier(SourceType).AddTrailingSpace(),
            SingleVariableDesignation(Identifier(typeArmVariableName))
        );

        var arm = SyntaxFactoryHelper.SwitchArm(declaration, Build(typeArmContext));
        if (discriminatorValue != null)
        {
            arm = arm.WithWhenClause(
                WhenClause(
                    BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        sourceDiscriminatorValue
                            ?? throw new InvalidOperationException(
                                $"Cannot use a {nameof(discriminatorValue)} if no {nameof(sourceDiscriminatorValue)} is set"
                            ),
                        discriminatorValue
                    )
                )
            );
        }

        return arm;
    }
}
