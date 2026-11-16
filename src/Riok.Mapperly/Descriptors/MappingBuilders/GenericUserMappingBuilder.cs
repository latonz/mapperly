using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.ExistingTarget;

namespace Riok.Mapperly.Descriptors.MappingBuilders;

/// <summary>
/// Resolves generic helpers when building a mapping body, without selecting the partial method being implemented.
/// </summary>
public static class GenericUserMappingBuilder
{
    public static INewInstanceMapping? TryBuildMapping(MappingBuilderContext ctx) =>
        CanDelegate(ctx) ? ctx.FindGenericMapping(ctx.MappingKey) ?? ctx.FindGenericMapping(ctx.MappingKey.TargetNonNullable()) : null;

    public static IExistingTargetMapping? TryBuildExistingTargetMapping(MappingBuilderContext ctx) =>
        CanDelegate(ctx) ? ctx.FindGenericExistingTargetMapping(ctx.MappingKey) : null;

    private static bool CanDelegate(MappingBuilderContext ctx)
    {
        // Explicit configuration describes a generated mapping body and must not be bypassed by a generic helper.
        var members = ctx.Configuration.Members;
        return !ctx.IsExpression
            && ctx.Configuration.DerivedTypes.Count == 0
            && members.ExplicitMappings.Count == 0
            && members.ValueMappings.Count == 0
            && members.NestedMappings.Count == 0
            && members.IgnoredSources.Count == 0
            && members.IgnoredTargets.Count == 0
            && !ctx.Configuration.Enum.HasExplicitConfigurations
            && (ctx.UserSymbol == null || !ctx.AttributeAccessor.HasAttribute<MapEnumAttribute>(ctx.UserSymbol))
            && ctx.UserMapping is not IParameterizedMapping { AdditionalSourceParameters.Count: > 0 };
    }
}
