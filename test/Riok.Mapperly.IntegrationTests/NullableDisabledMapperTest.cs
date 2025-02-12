using System.Threading.Tasks;
using Riok.Mapperly.IntegrationTests.Mapper;
using VerifyXunit;
using Xunit;

namespace Riok.Mapperly.IntegrationTests
{
    public class NullableDisabledMapperTest : BaseMapperTest
    {
        [Fact]
        public Task SnapshotGeneratedSource()
        {
            var path = GetGeneratedMapperFilePath(nameof(NullableDisabledMapper));
            return Verifier.VerifyFile(path);
        }

        [Fact]
        public Task RunMappingShouldWork()
        {
            var v = NullableDisabledMapper.Map(
                new NullableDisabledMapper.MyClass
                {
                    Nested = new NullableDisabledMapper.MyNestedClass(10),
                    IntValue = 11,
                    StringValue = "foo",
                }
            );
            return Verifier.Verify(v);
        }
    }
}
