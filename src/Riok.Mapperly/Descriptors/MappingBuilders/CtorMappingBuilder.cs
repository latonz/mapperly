using Microsoft.CodeAnalysis;
using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Common.Symbols;
using Riok.Mapperly.Descriptors.Mappings;

namespace Riok.Mapperly.Descriptors.MappingBuilders;

public static class CtorMappingBuilder
{
    public static CtorMapping? TryBuildMapping(MappingBuilderContext ctx)
    {
        if (!ctx.IsConversionEnabled(MappingConversionType.Constructor))
            return null;

        if (ctx.Target is not INamedTypeSymbol namedTarget)
            return null;

        // resolve ctors which have the source as single argument
        var ctor = namedTarget
            .InstanceConstructors.Where(ctx.SymbolAccessor.IsConstructorAccessible)
            .FirstOrDefault(m =>
                m.Parameters.Length == 1
                && SymbolEqualityComparer.Default.Equals(m.Parameters[0].Type.NonNullable(), ctx.Source.NonNullable())
                && ctx.Source.HasSameOrStricterNullability(m.Parameters[0].Type)
            );
        if (ctor == null)
            return null;

        return new CtorMapping(ctx.Source, ctx.Target, ctx.InstanceConstructors.BuildForConstructor(ctor));
    }
}
