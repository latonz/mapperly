using Microsoft.CodeAnalysis;
using Riok.Mapperly.Configuration;
using Riok.Mapperly.Descriptors.MappingBodyBuilders;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.DerivedTypes;
using Riok.Mapperly.Descriptors.Mappings.ExistingTarget;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings.SourceValue;
using Riok.Mapperly.Diagnostics;
using Riok.Mapperly.Helpers;
using Riok.Mapperly.Symbols.Members;

namespace Riok.Mapperly.Descriptors.MappingBuilders;

public static class DerivedTypeMappingBuilder
{
    public static INewInstanceMapping? TryBuildMapping(MappingBuilderContext ctx)
    {
        var derivedTypeMappings = TryBuildContainedMappings(ctx);
        if (derivedTypeMappings == null)
            return null;

        return ctx.IsExpression
            ? new DerivedTypeIfExpressionMapping(ctx.Source, ctx.Target, derivedTypeMappings)
            : new DerivedTypeSwitchMapping(ctx.Source, ctx.Target, derivedTypeMappings);
    }

    public static IExistingTargetMapping? TryBuildExistingTargetMapping(MappingBuilderContext ctx)
    {
        var derivedTypeMappings = TryBuildExistingTargetContainedMappings(ctx);
        return derivedTypeMappings == null ? null : new DerivedExistingTargetTypeSwitchMapping(ctx.Source, ctx.Target, derivedTypeMappings);
    }

    public static IReadOnlyCollection<INewInstanceMapping>? TryBuildContainedMappings(
        MappingBuilderContext ctx,
        bool duplicatedSourceTypesAllowed = false
    )
    {
        return ctx.Configuration.DerivedTypes == null
            ? null
            : BuildContainedMappings(ctx, ctx.Configuration.DerivedTypes, ctx.FindOrBuildMapping, duplicatedSourceTypesAllowed);
    }

    private static IReadOnlyCollection<IExistingTargetMapping>? TryBuildExistingTargetContainedMappings(
        MappingBuilderContext ctx,
        bool duplicatedSourceTypesAllowed = false
    )
    {
        return ctx.Configuration.DerivedTypes == null
            ? null
            : BuildContainedMappings(
                ctx,
                ctx.Configuration.DerivedTypes,
                (source, target, options, _) => ctx.FindOrBuildExistingTargetMapping(source, target, options),
                duplicatedSourceTypesAllowed
            );
    }

    private static IReadOnlyCollection<TMapping> BuildContainedMappings<TMapping>(
        MappingBuilderContext ctx,
        DerivedTypesMappingConfiguration config,
        Func<ITypeSymbol, ITypeSymbol, MappingBuildingOptions, Location?, TMapping?> findOrBuildMapping,
        bool duplicatedSourcesAllowed
    )
        where TMapping : ITypeMapping
    {
        MemberPath? typeDiscriminatorMember = null;
        if (
            config.TypeDiscriminatorMember != null
            && !ctx.SymbolAccessor.TryFindMemberPath(ctx.Source, config.TypeDiscriminatorMember, out typeDiscriminatorMember)
        )
        {
            // TODO diagnostic
            return [];
        }

        var derivedTypeMappingDiscriminators = new HashSet<(ITypeSymbol?, object?)>(
            new TupleEqualityComparer<ITypeSymbol?, object?>(SymbolTypeEqualityComparer.TypeDefault, EqualityComparer<object?>.Default)
        );
        var derivedTypeMappings = new List<TMapping>(config.DerivedTypeMappings.Count);

        foreach (var cfg in config.DerivedTypeMappings)
        {
            var sourceType = cfg.SourceType ?? ctx.Source;

            // set types non-nullable as they can never be null when type-switching.
            sourceType = sourceType.NonNullable();
            var targetType = cfg.TargetType.NonNullable();
            if (
                !duplicatedSourcesAllowed
                && !derivedTypeMappingDiscriminators.Add((sourceType, cfg.DiscriminatorValue?.ConstantValue.Value))
            )
            {
                ctx.ReportDiagnostic(DiagnosticDescriptors.DerivedSourceTypeDuplicated, sourceType);
                continue;
            }

            if (TryBuildDiscriminatorValue(ctx, cfg, typeDiscriminatorMember, out var discriminatorValue))
                continue;

            MemberPath? member = null;
            if (cfg.Member != null && !ctx.SymbolAccessor.TryFindMemberPath(sourceType, cfg.Member, out member))
            {
                // TODO diagnostic
                continue;
            }

            var typeCheckerResult = ctx.GenericTypeChecker.InferAndCheckTypes(
                ctx.UserSymbol!.TypeParameters,
                (ctx.Source, sourceType),
                (ctx.Target, targetType)
            );
            if (!typeCheckerResult.Success)
            {
                if (ReferenceEquals(sourceType, typeCheckerResult.FailedArgument))
                {
                    ctx.ReportDiagnostic(DiagnosticDescriptors.DerivedSourceTypeIsNotAssignableToParameterType, sourceType, ctx.Source);
                }
                else
                {
                    ctx.ReportDiagnostic(DiagnosticDescriptors.DerivedTargetTypeIsNotAssignableToReturnType, targetType, ctx.Target);
                }

                continue;
            }

            var mapping = findOrBuildMapping(
                sourceType,
                targetType,
                MappingBuildingOptions.KeepUserSymbol | MappingBuildingOptions.MarkAsReusable | MappingBuildingOptions.IgnoreDerivedTypes,
                cfg.Location
            );
            if (mapping == null)
            {
                ctx.ReportDiagnostic(DiagnosticDescriptors.CouldNotCreateMapping, sourceType, targetType);
                continue;
            }

            derivedTypeMappings.Add(
                new DerivedTypeMapping(
                    sourceType,
                    targetType,
                    cfg.SourceType != null,
                    discriminatorValue?.Value,
                    member?.BuildGetter(ctx),
                    mapping
                )
            );
        }

        return derivedTypeMappings;
    }

    private static bool TryBuildDiscriminatorValue(
        MappingBuilderContext ctx,
        DerivedTypeMappingConfiguration cfg,
        MemberPath? typeDiscriminatorMember,
        out ConstantSourceValue? discriminatorValue
    )
    {
        discriminatorValue = null;
        if (!cfg.DiscriminatorValue.HasValue)
            return true;

        if (typeDiscriminatorMember == null)
        {
            // TODO diagnostic
            return false;
        }

        if (
            !SourceValueBuilder.TryBuildConstantSourceValue(
                ctx,
                cfg.DiscriminatorValue.Value,
                typeDiscriminatorMember,
                out discriminatorValue
            )
        )
        {
            // TODO diagnostic
            return false;
        }

        return true;
    }
}
