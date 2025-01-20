using System.Diagnostics;

namespace Riok.Mapperly.Abstractions;

/// <summary>
/// Specifies derived type mappings for which a mapping should be generated.
/// A type switch is implemented over the source object and the provided source types.
/// Each source type has to be unique but multiple source types can be mapped to the same target type.
/// Each source type needs to extend or implement the parameter type of the mapping method.
/// Each target type needs to extend or implement the return type of the mapping method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[Conditional("MAPPERLY_ABSTRACTIONS_SCOPE_RUNTIME")]
public sealed class MapDerivedTypeAttribute : Attribute
{
    /// <summary>
    /// Registers a derived type mapping.
    /// </summary>
    /// <param name="sourceType">The derived source type.</param>
    /// <param name="targetType">The derived target type.</param>
    public MapDerivedTypeAttribute(Type sourceType, Type targetType)
    {
        SourceType = sourceType;
        TargetType = targetType;
    }

    public MapDerivedTypeAttribute(Type targetType, object? discriminatorValue)
    {
        TargetType = targetType;
        DiscriminatorValue = discriminatorValue;
    }

    public MapDerivedTypeAttribute(Type targetType, object? discriminatorValue, string member)
    {
        TargetType = targetType;
        DiscriminatorValue = discriminatorValue;
    }

    public MapDerivedTypeAttribute(Type targetType, object? discriminatorValue, string[] member)
    {
        TargetType = targetType;
        DiscriminatorValue = discriminatorValue;
    }

    /// <summary>
    /// Gets the source type of the derived type mapping.
    /// </summary>
    public Type? SourceType { get; }

    /// <summary>
    /// Gets the target type of the derived type mapping.
    /// </summary>
    public Type TargetType { get; }

    public object? DiscriminatorValue { get; }

    public IReadOnlyCollection<string>? Member { get; }

    public string? MemberFullName { get; }
}

/// <summary>
/// Specifies derived type mappings for which a mapping should be generated.
/// A type switch is implemented over the source object and the provided source types.
/// Each source type has to be unique but multiple source types can be mapped to the same target type.
/// Each source type needs to extend or implement the parameter type of the mapping method.
/// Each target type needs to extend or implement the return type of the mapping method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[Conditional("MAPPERLY_ABSTRACTIONS_SCOPE_RUNTIME")]
public sealed class MapDerivedTypeAttribute<TSource, TTarget> : Attribute;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[Conditional("MAPPERLY_ABSTRACTIONS_SCOPE_RUNTIME")]
public sealed class MapDerivedTypeAttribute<TTarget> : Attribute
{
    public MapDerivedTypeAttribute(object? discriminatorValue, string member)
    {
        DiscriminatorValue = discriminatorValue;
    }

    public MapDerivedTypeAttribute(object? discriminatorValue, string[] member)
    {
        DiscriminatorValue = discriminatorValue;
    }

    public object? DiscriminatorValue { get; }

    public IReadOnlyCollection<string>? Member { get; }

    public string? MemberFullName { get; }
}
