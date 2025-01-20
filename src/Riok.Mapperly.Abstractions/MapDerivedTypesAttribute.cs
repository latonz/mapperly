using System.Diagnostics;

namespace Riok.Mapperly.Abstractions;

[AttributeUsage(AttributeTargets.Method)]
[Conditional("MAPPERLY_ABSTRACTIONS_SCOPE_RUNTIME")]
public sealed class MapDerivedTypesAttribute : Attribute
{
    public MapDerivedTypesAttribute(string[] discriminatorMember)
    {
        TypeDiscriminatorMember = discriminatorMember;
    }

    /// <summary>
    /// Gets the name of the type discriminator member.
    /// </summary>
    public IReadOnlyCollection<string> TypeDiscriminatorMember { get; }
}
