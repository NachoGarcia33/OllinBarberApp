using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace OllinBarberApp.Models
{
    public class Producto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(800)]
        public string Descripcion { get; set; } = string.Empty;

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        [StringLength(80)]
        public string Categoria { get; set; } = string.Empty;

        [StringLength(80)]
        public string Marca { get; set; } = string.Empty;

        [Range(0.01, 999999999, ErrorMessage = "El precio debe ser mayor que cero.")]
        public decimal Precio { get; set; }

        [Range(0, 100, ErrorMessage = "El descuento debe estar entre 0 y 100%.")]
        public decimal DescuentoPorcentaje { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        [StringLength(300)]
        public string ImagenUrl { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [NotMapped]
        public decimal PrecioFinal => decimal.Round(
            Precio * (1 - (DescuentoPorcentaje / 100m)), 0, MidpointRounding.AwayFromZero);
    }
}
