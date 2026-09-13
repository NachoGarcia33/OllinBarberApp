using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class CarritoItemViewModel
    {
        public Producto Producto { get; set; } = new();
        public int Cantidad { get; set; }
        public decimal PrecioUnitario => Producto.PrecioFinal;
        public decimal Subtotal => PrecioUnitario * Cantidad;
        public decimal Ahorro => (Producto.Precio - Producto.PrecioFinal) * Cantidad;
    }

    public class CarritoViewModel
    {
        public List<CarritoItemViewModel> Items { get; set; } = new();
        public decimal Subtotal => Items.Sum(i => i.Producto.Precio * i.Cantidad);
        public decimal DescuentoTotal => Items.Sum(i => i.Ahorro);
        public decimal Total => Items.Sum(i => i.Subtotal);
        public int CantidadTotal => Items.Sum(i => i.Cantidad);
    }

    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        [Display(Name = "Nombre completo")]
        public string ClienteNombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
        [StringLength(160)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "El celular es obligatorio.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Ingresa un celular colombiano de 10 dígitos.")]
        public string Telefono { get; set; } = string.Empty;

        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(180)]
        public string Direccion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ciudad es obligatoria.")]
        [StringLength(80)]
        public string Ciudad { get; set; } = string.Empty;

        [Required(ErrorMessage = "Selecciona un método de pago.")]
        public string MetodoPago { get; set; } = "Contra entrega";

        [Required(ErrorMessage = "Selecciona un método de entrega.")]
        public string MetodoEntrega { get; set; } = "Recoger en barbería";

        [StringLength(400)]
        public string? Notas { get; set; }

        public CarritoViewModel Carrito { get; set; } = new();
    }

    public class SeguimientoPedidoViewModel
    {
        [Required(ErrorMessage = "Ingresa el código del pedido.")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ingresa el celular usado en la compra.")]
        [RegularExpression(@"^[0-9]{10}$", ErrorMessage = "Ingresa un celular de 10 dígitos.")]
        public string Telefono { get; set; } = string.Empty;

        public Pedido? Pedido { get; set; }
    }
}
