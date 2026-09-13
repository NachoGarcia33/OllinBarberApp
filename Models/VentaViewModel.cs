using System.ComponentModel.DataAnnotations;

namespace OllinBarberApp.Models
{
    public class VentaViewModel
    {
        [Required(ErrorMessage = "El nombre del cliente es obligatorio.")]
        [StringLength(100)]
        public string ClienteNombre { get; set; } = string.Empty;

        public List<int> ProductosIds { get; set; } = new();
        public List<int> Cantidades { get; set; } = new();
        public List<Producto> ProductosDisponibles { get; set; } = new();
    }
}
