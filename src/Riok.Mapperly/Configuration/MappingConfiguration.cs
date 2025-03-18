using Riok.Mapperly.Abstractions;
using Riok.Mapperly.Common.Syntax;

namespace Riok.Mapperly.Configuration;

public record MappingConfiguration(
    MapperAttribute Mapper,
    EnumMappingConfiguration Enum,
    MembersMappingConfiguration Members,
    IReadOnlyCollection<DerivedTypeMappingConfiguration> DerivedTypes,
    bool UseDeepCloning,
    SupportedFeatures SupportedFeatures
);
