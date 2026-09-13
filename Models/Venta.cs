using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class Venta
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ClienteNombre { get; set; } = string.Empty;

        public DateTime Fecha { get; set; }

        [Range(0, 999999999)]
        public decimal Total { get; set; }

        public List<VentaDetalle> Detalles { get; set; } = new();
    }
}
