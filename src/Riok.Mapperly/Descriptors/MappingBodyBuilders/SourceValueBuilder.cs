using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Riok.Mapperly.Configuration;
using Riok.Mapperly.Descriptors.MappingBodyBuilders.BuilderContext;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings.SourceValue;
using Riok.Mapperly.Diagnostics;
using Riok.Mapperly.Helpers;
using Riok.Mapperly.Symbols.Members;
using static Riok.Mapperly.Emit.Syntax.SyntaxFactoryHelper;

namespace Riok.Mapperly.Descriptors.MappingBodyBuilders;

internal static class SourceValueBuilder
{
    /// <summary>
    /// Tries to build an <see cref="ISourceValue"/> instance which serializes as an expression,
    /// with a value that is assignable to the target member requested in the <paramref name="memberMappingInfo"/>.
    /// </summary>
    public static bool TryBuildMappedSourceValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    ) => TryBuildMappedSourceValue(ctx, memberMappingInfo, MemberMappingBuilder.CodeStyle.Expression, out sourceValue);

    /// <summary>
    /// Tries to build an <see cref="ISourceValue"/> instance,
    /// with a value that is assignable to the target member requested in the <paramref name="memberMappingInfo"/>.
    /// </summary>
    public static bool TryBuildMappedSourceValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        MemberMappingBuilder.CodeStyle codeStyle,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    )
    {
        if (memberMappingInfo.ValueConfiguration != null)
            return TryBuildValue(ctx, memberMappingInfo, out sourceValue);

        if (memberMappingInfo.SourceMember != null)
            return MemberMappingBuilder.TryBuild(ctx, memberMappingInfo, codeStyle, out sourceValue);

        sourceValue = null;
        return false;
    }

    private static bool TryBuildValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    )
    {
        // always set the member mapped,
        // as other diagnostics are reported if the mapping fails to be built
        ctx.SetMembersMapped(memberMappingInfo);

        if (memberMappingInfo.ValueConfiguration!.Value != null)
        {
            return TryBuildConstantSourceValue(
                ctx.BuilderContext,
                memberMappingInfo.ValueConfiguration.Value.Value,
                memberMappingInfo.TargetMember,
                out sourceValue
            );
        }

        if (memberMappingInfo.ValueConfiguration!.Use != null)
            return TryBuildMethodProvidedSourceValue(ctx, memberMappingInfo, out sourceValue);

        throw new InvalidOperationException($"Illegal {nameof(MemberValueMappingConfiguration)}");
    }

    internal static bool TryBuildConstantSourceValue(
        MappingBuilderContext ctx,
        AttributeValue value,
        MemberPath targetMember,
        [NotNullWhen(true)] out ConstantSourceValue? sourceValue
    )
    {
        // the target is a non-nullable reference type,
        // but the provided value is null or default (for default IsNullable is also true)
        if (value.ConstantValue.IsNull && targetMember.MemberType.IsReferenceType && !targetMember.MemberType.IsNullable())
        {
            ctx.ReportDiagnostic(DiagnosticDescriptors.CannotMapValueNullToNonNullable, targetMember.ToDisplayString());
            sourceValue = new ConstantSourceValue(SuppressNullableWarning(value.Expression));
            return true;
        }

        // target is value type but value is null
        if (
            value.ConstantValue.IsNull
            && targetMember.MemberType.IsValueType
            && !targetMember.MemberType.IsNullableValueType()
            && value.Expression.IsKind(SyntaxKind.NullLiteralExpression)
        )
        {
            ctx.ReportDiagnostic(DiagnosticDescriptors.CannotMapValueNullToNonNullable, targetMember.ToDisplayString());
            sourceValue = new ConstantSourceValue(DefaultLiteral());
            return true;
        }

        // the target accepts null and the value is null or default
        // use the expression instant of a constant null literal
        // to use "default" or "null" depending on what the user specified in the attribute
        if (value.ConstantValue.IsNull)
        {
            sourceValue = new ConstantSourceValue(value.Expression);
            return true;
        }

        // use non-nullable target type to allow non-null value type assignments
        // to nullable value types
        if (!SymbolEqualityComparer.Default.Equals(value.ConstantValue.Type, targetMember.MemberType.NonNullable()))
        {
            ctx.ReportDiagnostic(
                DiagnosticDescriptors.MapValueTypeMismatch,
                value.Expression.ToFullString(),
                value.ConstantValue.Type?.ToDisplayString() ?? "unknown",
                targetMember.ToDisplayString()
            );
            sourceValue = null;
            return false;
        }

        switch (value.ConstantValue.Kind)
        {
            case TypedConstantKind.Primitive:
                sourceValue = new ConstantSourceValue(value.Expression);
                return true;
            case TypedConstantKind.Enum:
                // expand enum member access to fully qualified identifier
                // use simple member name approach instead of slower visitor pattern on the expression
                var enumMemberName = ((MemberAccessExpressionSyntax)value.Expression).Name.Identifier.Text;
                var enumTypeFullName = FullyQualifiedIdentifier(targetMember.MemberType.NonNullable());
                sourceValue = new ConstantSourceValue(MemberAccess(enumTypeFullName, enumMemberName));
                return true;
            case TypedConstantKind.Type:
            case TypedConstantKind.Array:
                ctx.ReportDiagnostic(DiagnosticDescriptors.MapValueUnsupportedType, value.ConstantValue.Kind.ToString());
                break;
        }

        sourceValue = null;
        return false;
    }

    private static bool TryBuildMethodProvidedSourceValue(
        IMembersBuilderContext<IMapping> ctx,
        MemberMappingInfo memberMappingInfo,
        [NotNullWhen(true)] out ISourceValue? sourceValue
    )
    {
        if (ValidateValueProviderMethod(ctx, memberMappingInfo))
        {
            sourceValue = new MethodProvidedSourceValue(memberMappingInfo.ValueConfiguration!.Use!);
            return true;
        }

        sourceValue = null;
        return false;
    }

    private static bool ValidateValueProviderMethod(IMembersBuilderContext<IMapping> ctx, MemberMappingInfo memberMappingInfo)
    {
        var methodName = memberMappingInfo.ValueConfiguration!.Use!;
        var namedMethodCandidates = ctx
            .BuilderContext.MapperDeclaration.Symbol.GetMembers(methodName)
            .OfType<IMethodSymbol>()
            .Where(m => m is { IsAsync: false, ReturnsVoid: false, IsGenericMethod: false, Parameters.Length: 0 })
            .ToList();

        if (namedMethodCandidates.Count == 0)
        {
            ctx.BuilderContext.ReportDiagnostic(DiagnosticDescriptors.MapValueReferencedMethodNotFound, methodName);
            return false;
        }

        // use non-nullable to allow non-null value type assignments
        // to nullable value types
        // nullable is checked with nullable annotation
        var methodCandidates = namedMethodCandidates.Where(x =>
            SymbolEqualityComparer.Default.Equals(x.ReturnType.NonNullable(), memberMappingInfo.TargetMember.MemberType.NonNullable())
        );

        if (!memberMappingInfo.TargetMember.Member.IsNullable)
        {
            // only assume annotated is nullable, none is threated as non-nullable here
            methodCandidates = methodCandidates.Where(m => m.ReturnNullableAnnotation != NullableAnnotation.Annotated);
        }

        var method = methodCandidates.FirstOrDefault();
        if (method != null)
            return true;

        ctx.BuilderContext.ReportDiagnostic(
            DiagnosticDescriptors.MapValueMethodTypeMismatch,
            methodName,
            namedMethodCandidates[0].ReturnType.ToDisplayString(),
            memberMappingInfo.TargetMember.ToDisplayString()
        );
        return false;
    }
}
