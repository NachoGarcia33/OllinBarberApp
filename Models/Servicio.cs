using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace OllinBarberApp.Models
{
    public class Servicio
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(120)]
        public string Nombre { get; set; } = string.Empty;

        [Range(1, 480, ErrorMessage = "La duración debe estar entre 1 y 480 minutos.")]
        public int Duracion { get; set; }

        [Range(0, 999999999, ErrorMessage = "El precio no puede ser negativo.")]
        public decimal Precio { get; set; }

        [StringLength(80)]
        public string Tipo { get; set; } = string.Empty;

        public bool Activo { get; set; } = true;

        [StringLength(300)]
        public string? ImagenUrl { get; set; }

        [ValidateNever]
        public ICollection<Cita> Citas { get; set; } = new List<Cita>();
    }
}
