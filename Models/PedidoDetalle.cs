using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class PedidoDetalle
    {
        public int Id { get; set; }
        public int PedidoId { get; set; }
        public Pedido? Pedido { get; set; }
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }
        public decimal DescuentoUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}
