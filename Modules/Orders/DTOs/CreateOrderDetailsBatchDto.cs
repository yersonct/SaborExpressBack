namespace SaborExpress.Modules.Orders.DTOs
{
    // El mesero manda todos los productos de una "tanda" en una sola
    // petición; el backend les asigna el mismo BatchNumber a todos,
    // para que cocina los vea como un ticket separado.
    public class CreateOrderDetailsBatchDto
    {
        public List<CreateOrderDetailDto> Items { get; set; } = new();
    }
}