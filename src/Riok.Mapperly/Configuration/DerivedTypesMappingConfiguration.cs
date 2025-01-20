namespace Riok.Mapperly.Configuration;

// TODO docs
public record DerivedTypesMappingConfiguration(IMemberPathConfiguration? TypeDiscriminatorMember = null)
{
    public IReadOnlyCollection<DerivedTypeMappingConfiguration> DerivedTypeMappings { get; set; } = [];
}
