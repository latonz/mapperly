namespace Riok.Mapperly.IntegrationTests.Models
{
    public class InitOnlyPathAssignmentSource
    {
        public int Value { get; set; }
    }

    public class InitOnlyPathAssignmentNested
    {
        public int Value { get; set; }
    }

    public class InitOnlyPathAssignmentTarget
    {
        public InitOnlyPathAssignmentNested? Nested { get; init; }
    }

    public class RequiredPathAssignmentTarget
    {
        public required InitOnlyPathAssignmentNested Nested { get; set; }
    }
}
