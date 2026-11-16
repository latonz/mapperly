using Riok.Mapperly.Descriptors.MappingBuilders;
using Riok.Mapperly.Descriptors.Mappings;
using Riok.Mapperly.Descriptors.Mappings.MemberMappings;
using Riok.Mapperly.Descriptors.Mappings.UserMappings;

namespace Riok.Mapperly.Descriptors.MappingBodyBuilders;

/// <summary>
/// Builds bodies mappings (the body of the mapping methods).
/// </summary>
public class MappingBodyBuilder(MappingCollection mappings)
{
    public void BuildMappingBodies(CancellationToken cancellationToken)
    {
        var runtimeMappingBuilders = new List<Action>();
        foreach (var (typeMapping, ctx) in mappings.DequeueMappingsToBuildBody())
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (typeMapping)
            {
                case INewInstanceEnumerableMapping mapping:
                    EnumerableMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case IEnumerableMapping mapping:
                    EnumerableMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case NewInstanceObjectMemberMethodMapping mapping:
                    NewInstanceObjectMemberMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case NewInstanceObjectMemberMapping mapping:
                    NewInstanceObjectMemberMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case NewValueTupleExpressionMapping mapping:
                    NewValueTupleMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case NewValueTupleConstructorMapping mapping:
                    NewValueTupleMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case IMemberAssignmentTypeMapping mapping:
                    ObjectMemberMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case UserDefinedNewInstanceMethodMapping mapping:
                    UserMethodMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case UserDefinedExistingTargetMethodMapping mapping:
                    UserMethodMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case UserDefinedExpressionMethodMapping mapping:
                    ExpressionMappingBodyBuilder.BuildMappingBody(ctx, mapping);
                    break;
                case UserDefinedNewInstanceGenericTypeMapping mapping:
                    var genericDerivedMappings = DerivedTypeMappingBuilder.TryBuildContainedMappings(ctx, true);
                    runtimeMappingBuilders.Add(() =>
                        RuntimeTargetTypeMappingBodyBuilder.BuildMappingBody(ctx, mapping, genericDerivedMappings)
                    );
                    break;
                case UserDefinedNewInstanceRuntimeTargetTypeMapping mapping:
                    var derivedMappings = DerivedTypeMappingBuilder.TryBuildContainedMappings(ctx, true);
                    runtimeMappingBuilders.Add(() => RuntimeTargetTypeMappingBodyBuilder.BuildMappingBody(ctx, mapping, derivedMappings));
                    break;
                case UserDefinedExistingTargetGenericTypeMapping mapping:
                    var existingTargetDerivedMappings = DerivedTypeMappingBuilder.TryBuildExistingTargetContainedMappings(ctx, true);
                    runtimeMappingBuilders.Add(() =>
                        RuntimeTargetTypeMappingBodyBuilder.BuildMappingBody(ctx, mapping, existingTargetDerivedMappings)
                    );
                    break;
            }
        }

        // Nested mappings and explicit derived type registrations can discover concrete generic helpers.
        // Build runtime switches only after this queue (including newly enqueued mappings) has been drained.
        foreach (var buildRuntimeMapping in runtimeMappingBuilders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            buildRuntimeMapping();
        }
    }
}
