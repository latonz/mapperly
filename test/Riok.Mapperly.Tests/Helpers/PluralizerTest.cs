using Riok.Mapperly.Helpers;

namespace Riok.Mapperly.Tests.Helpers;

public class PluralizerTest
{
    [Theory]
    [InlineData("buses", "bus", true)]
    [InlineData("boxes", "box", true)]
    [InlineData("leaves", "leaf", true)]
    [InlineData("potatoes", "potato", true)]
    [InlineData("cats", "cat", true)]
    [InlineData("dogs", "dog", true)]
    [InlineData("sheep", null, false)]
    [InlineData("", null, false)]
    public void TryToSingularShouldReturnExpectedResults(string plural, string? expectedSingular, bool expectedResult)
    {
        var result = Pluralizer.TryToSingular(plural, out var singular);
        result.Should().Be(expectedResult);
        singular.Should().Be(expectedSingular);
    }
}
