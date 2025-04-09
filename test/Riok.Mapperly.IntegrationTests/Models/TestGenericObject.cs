namespace Riok.Mapperly.IntegrationTests.Models
{
    public class TestGenericObject<T>
        where T : struct
    {
        private T Id { get; set; } = default!;
    }
}
