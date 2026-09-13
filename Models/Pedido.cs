using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class Pedido
    {
        public int Id { get; set; }

        [Required, StringLength(24)]
        public string Codigo { get; set; } = string.Empty;

        public string? UsuarioId { get; set; }

        [Required, StringLength(100)]
        public string ClienteNombre { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(160)]
        public string Email { get; set; } = string.Empty;

        [Required, RegularExpression(@"^[0-9]{10}$"), StringLength(10)]
        public string Telefono { get; set; } = string.Empty;

        [Required, StringLength(180)]
        public string Direccion { get; set; } = string.Empty;

        [Required, StringLength(80)]
        public string Ciudad { get; set; } = string.Empty;

        [Required, StringLength(40)]
        public string MetodoPago { get; set; } = string.Empty;

        [Required, StringLength(40)]
        public string MetodoEntrega { get; set; } = string.Empty;

        [StringLength(400)]
        public string? Notas { get; set; }

        public DateTimeOffset Fecha { get; set; }

        public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;

        public decimal Subtotal { get; set; }

        public decimal DescuentoTotal { get; set; }

        public decimal Total { get; set; }

        public List<PedidoDetalle> Detalles { get; set; } = new();
    }
}
