using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings;
using Riok.Mapperly.Diagnostics;
using Riok.Mapperly.Symbols.Members;

namespace Riok.Mapperly.Descriptors.MappingBodyBuilders.BuilderContext;

/// <summary>
/// An implementation of an <see cref="INewInstanceBuilderContext{T}"/>
/// which supports containers (<seealso cref="MembersContainerBuilderContext{T}"/>).
/// </summary>
/// <typeparam name="T"></typeparam>
public class NewInstanceContainerBuilderContext<T>(MappingBuilderContext builderContext, T mapping)
    : MembersContainerBuilderContext<T>(builderContext, mapping),
        INewInstanceBuilderContext<T>
    where T : INewInstanceObjectMemberMapping, IMemberAssignmentTypeMapping
{
    public void AddConstructorParameterMapping(ConstructorParameterMapping mapping)
    {
        Mapping.AddConstructorParameterMapping(mapping);
        MappingAdded(mapping.MemberInfo, true);
    }

    public void AddInitMemberMapping(MemberAssignmentMapping mapping)
    {
        Mapping.AddInitMemberMapping(mapping);
        MappingAdded(mapping.MemberInfo);
    }

    public bool TryMatchInitOnlyMember(
        IMappableMember targetMember,
        [NotNullWhen(true)] out MemberMappingInfo? memberInfo,
        out bool hasPathConfigs
    )
    {
        hasPathConfigs = false;

        if (TryMatchMember(targetMember, out memberInfo))
            return true;

        // Path configs exist for this init member.
        // Don't reject: the member will be initialized with a new instance in the initializer,
        // and the path configs will be handled as regular member assignments after construction.
        if (TryGetMemberValueConfigs(targetMember.Name, false, out _))
        {
            hasPathConfigs = true;
            return false;
        }

        if (TryGetMemberConfigs(targetMember.Name, false, out _))
        {
            hasPathConfigs = true;
            return false;
        }

        return false;
    }

    public bool TryMatchParameter(IParameterSymbol parameter, [NotNullWhen(true)] out MemberMappingInfo? memberInfo) =>
        TryMatchMember(new ConstructorParameterMember(parameter, BuilderContext.SymbolAccessor), true, out memberInfo);
}
