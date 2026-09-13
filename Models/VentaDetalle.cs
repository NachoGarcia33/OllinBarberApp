using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class VentaDetalle
    {
        public int Id { get; set; }
        public int VentaId { get; set; }
        public Venta? Venta { get; set; }
        public int ProductoId { get; set; }
        public Producto? Producto { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; }

        [Range(0, 999999999)]
        public decimal PrecioUnitario { get; set; }

        [Range(0, 999999999)]
        public decimal Subtotal { get; set; }
    }
}
