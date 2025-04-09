namespace Riok.Mapperly.IntegrationTests.Dto
{
    public class TestGenericObjectDto<T>
        where T : struct
    {
        private T Id { get; set; } = default!;
    }
}
