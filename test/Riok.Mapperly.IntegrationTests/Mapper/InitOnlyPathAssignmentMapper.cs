using Riok.Mapperly.Abstractions;
using Riok.Mapperly.IntegrationTests.Models;

namespace Riok.Mapperly.IntegrationTests.Mapper
{
    [Mapper]
    public partial class InitOnlyPathAssignmentMapper
    {
        [MapProperty(
            nameof(InitOnlyPathAssignmentSource.Value),
            nameof(InitOnlyPathAssignmentTarget.Nested) + "." + nameof(InitOnlyPathAssignmentNested.Value)
        )]
        public partial InitOnlyPathAssignmentTarget Map(InitOnlyPathAssignmentSource source);

        [MapProperty(
            nameof(InitOnlyPathAssignmentSource.Value),
            nameof(RequiredPathAssignmentTarget.Nested) + "." + nameof(InitOnlyPathAssignmentNested.Value)
        )]
        public partial RequiredPathAssignmentTarget MapRequired(InitOnlyPathAssignmentSource source);
    }
}
