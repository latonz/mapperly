using Riok.Mapperly.IntegrationTests.Mapper;
using Riok.Mapperly.IntegrationTests.Models;
using Shouldly;
using Xunit;

namespace Riok.Mapperly.IntegrationTests
{
    public class InitOnlyPathAssignmentMapperTest : BaseMapperTest
    {
        [Fact]
        public void MapToInitOnlyPathAssignmentShouldWork()
        {
            var source = new InitOnlyPathAssignmentSource { Value = 42 };
            var target = new InitOnlyPathAssignmentMapper().Map(source);

            target.Nested.ShouldNotBeNull();
            target.Nested.Value.ShouldBe(42);
        }

        [Fact]
        public void MapToRequiredPathAssignmentShouldWork()
        {
            var source = new InitOnlyPathAssignmentSource { Value = 84 };
            var target = new InitOnlyPathAssignmentMapper().MapRequired(source);

            target.Nested.ShouldNotBeNull();
            target.Nested.Value.ShouldBe(84);
        }
    }
}
